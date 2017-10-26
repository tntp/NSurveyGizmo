using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Xml;
using Newtonsoft.Json;

namespace NSurveyGizmo
{
    public interface IThrottledWebRequest
    {
        TimeSpan? MinimumTimeBetweenRequests { get; set; }
        DateTime? LastRequestStartTime { get; set; }
        IThrottledWebRequest Create(string requestUriString);
        WebResponse GetResponse();
        XmlDocument GetXmlDocument(string url);
        T GetJsonObject<T>(string url);
    }

    /// <summary>
    /// we experienced issues (account banned, ...) with the API when hammering it with requests
    /// called support and they told us to throttle our requests
    /// this class was added in response to that
    /// YMMV
    /// </summary>
    public class ThrottledWebRequest : IThrottledWebRequest
    {
        public DateTime? LastRequestStartTime { get; set; }
        public TimeSpan? MinimumTimeBetweenRequests { get; set; }
        public WebRequest WebRequest { get; set; }
        public TimeSpan DefaultMinimumTimeBetweenRequests = new TimeSpan(0, 0, 0, 0, 1000);

        public ThrottledWebRequest(TimeSpan? minimumTimeBetweenRequests = null)
        {
            MinimumTimeBetweenRequests = minimumTimeBetweenRequests ?? DefaultMinimumTimeBetweenRequests;
        }

        public ThrottledWebRequest(WebRequest webRequest, TimeSpan? minimumTimeBetweenRequests = null)
        {
            WebRequest = webRequest;
            MinimumTimeBetweenRequests = minimumTimeBetweenRequests ?? DefaultMinimumTimeBetweenRequests;
        }

        public WebResponse GetResponse()
        {
            return WebRequest.GetResponse();
        }

        public void Sleep()
        {
            var now = DateTime.Now;
            var requestStartTime = LastRequestStartTime ?? now;
            var timeBetweenRequests = MinimumTimeBetweenRequests ?? new TimeSpan(0);
            var requestTime = now - requestStartTime;

            // if it's been at least the minimum time between requests, no need to sleep
            if (requestTime >= timeBetweenRequests) return;

            // sleep for whatever amount of the minimum duration is left
            var sleepTime = timeBetweenRequests - requestTime;
            Thread.Sleep(sleepTime);
        }

        public IThrottledWebRequest Create(string requestUriString)
        {
            Sleep();
            LastRequestStartTime = DateTime.Now;
            return new ThrottledWebRequest(WebRequest.Create(requestUriString), MinimumTimeBetweenRequests)
            {
                LastRequestStartTime = LastRequestStartTime
            };
        }

        public XmlDocument GetXmlDocument(string url)
        {
            var request = Create(url);
            var response = request.GetResponse();
            var doc = new XmlDocument();
            var responseStream = response.GetResponseStream();
            if (responseStream == null)
            {
                throw new WebException("Unable to open response stream from SurveyGizmo.");
            }
            doc.Load(responseStream);
            return doc;
        }

        public T GetJsonObject<T>(string url)
        {
            // TODO: add more null checks here
            var responseStream = Create(url).GetResponse().GetResponseStream();
            if (responseStream == null)
            {
                throw new WebException("Unable to open response stream from SurveyGizmo.");
            }

            using (var sr = new StreamReader(responseStream))
            {
                var result = sr.ReadToEnd();
                return JsonConvert.DeserializeObject<T>(result);
            }
        }
    }
}