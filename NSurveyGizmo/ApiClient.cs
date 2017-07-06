using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using NLog;
using Polly;

namespace NSurveyGizmo
{
    public class ApiClient
    {
        public IThrottledWebRequest ThrottledWebRequest = new ThrottledWebRequest();
        public int? BatchSize = null;
        public string BaseServiceUrl = "https://restapi.surveygizmo.com/v4/";
        public string ApiToken { get; set; }
        public string ApiTokenSecret { get; set; }
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        #region questions

        public List<SurveyQuestion> GetQuestions(int surveyId, bool getAllPages = true)
        {
            return GetData<SurveyQuestion>("survey/" + surveyId + "/surveyquestion", getAllPages, true);
        }
        public SurveyQuestion CreateQuestion(int surveyId, int surveyPage, string type, string title, bool? required, List<string> subTypeProps)
        {
            StringBuilder url = new StringBuilder("survey/" + surveyId + "/surveypage/" + surveyPage + "/surveyquestion?_method=PUT");

            if (!String.IsNullOrEmpty(type))
            {
                url.Append("&type=" + Uri.EscapeDataString(type));
            }
            if (!String.IsNullOrEmpty(title))
            {
                url.Append("&title=" + Uri.EscapeDataString(title));
            }
            if (required != null)
            {
                url.Append("&properties[required]=" + required);
            }
            if (subTypeProps != null && subTypeProps.Count > 0)
            {
                var counter = 1;
                url.Append("&properties[subtype]=");
                foreach (var prop in subTypeProps)
                {
                    if (counter == 1)
                    {
                        url.Append(Uri.EscapeDataString(prop));
                    }
                    else
                    {
                        url.Append("," + Uri.EscapeDataString(prop));
                    }
                    counter = counter + 1;
                }
            }

            var response = GetData<SurveyQuestion>(url.ToString());
            return response[0];
        }
        #endregion

        #region responses

        public List<SurveyResponse> GetResponses(int surveyId, bool getAllPages = true)
        {
            return GetData<SurveyResponse>("survey/" + surveyId + "/surveyresponse", getAllPages, true);
        }

        #endregion

        #region surveys

        public List<Survey> GetAllSurveys(bool getAllPages = true)
        {
            return GetData<Survey>("survey", getAllPages, true);
        }

        public int CreateSurvey(string title)
        {
            var surveys = GetData<Survey>("survey/?_method=PUT&type=survey&title=" + Uri.EscapeDataString(title ?? ""));
            // TODO: return the survey object?
            if (surveys == null || surveys.Count < 1) return 0;
            return surveys[0].id;
        }

        public bool DeleteSurvey(int surveyId)
        {
            var results = GetData<Result>("survey/" + surveyId + "?_method=DELETE", nonQuery: true);
            return ResultOk(results);
        }

        public Survey GetSurvey(int id)
        {
            var results = GetData<Survey>("survey/" + id);
            return results != null && results.Count > 0 ? results[0] : null;
        }

        #endregion

        #region campaigns

        public int CreateCampaign(int surveyId, string campaignName, int masterCampaignId = 0)
        {
            var method = "PUT";
            var id = "";
            var type = "&type=email";
            var copy = "";

            if (masterCampaignId > 0)
            {
                method = "POST";
                id = "/" + masterCampaignId;
                type = "";
                copy = "&copy=true";
            }

            var campaigns = GetData<SurveyCampaign>("survey/" + surveyId + "/surveycampaign" + id + "?_method=" + method + type + "&name=" +
                                  Uri.EscapeDataString(campaignName ?? "") + copy);

            // TODO: return campaign object?

            if (campaigns == null || campaigns.Count < 1) return 0;
            return campaigns[0].id;
        }

        public List<SurveyCampaign> GetCampaigns(int surveyId, bool getAllPages = true)
        {
            return GetData<SurveyCampaign>("survey/" + surveyId + "/surveycampaign", getAllPages, true);
        }

        public SurveyCampaign GetCampaign(int surveyId, int campaignId)
        {
            var results = GetData<SurveyCampaign>("survey/" + surveyId + "/surveycampaign/" + campaignId);
            return results != null && results.Count > 0 ? results[0] : null;
        }

        public bool DeleteCampaign(int surveyId, int campaignId)
        {
            var results = GetData<Result>("survey/" + surveyId + "/surveycampaign/" + campaignId + "?_method=DELETE", nonQuery: true);
            return ResultOk(results);
        }

        public bool UpdateCampaign(int surveyId, SurveyCampaign campaign)
        {
            var url = BuildUrl("survey/" + surveyId + "/surveycampaign/" + campaign.id + "?_method=POST",
                new Dictionary<string, string>() { { "name", campaign.name }, { "status", campaign.status } });

            // TODO: allow updating the rest of the properties of a campaign

            var results = GetData<Result>(url.ToString(), nonQuery: true);

            return ResultOk(results);
        }
        public bool UpdateQcodeOfSurveyQuestion(int surveyId, int questionId, string qCode)
        {
            qCode = $"<span style=\"font-size:0px;\">{qCode}</span>";

            var url = new StringBuilder("survey/" + surveyId + "/surveyquestion/" + questionId + "?_method=POST&properties[question_description][English]=" + Uri.EscapeUriString(qCode));

            var results = GetData<Result>(url.ToString(), nonQuery: true);

            return ResultOk(results);
        }
        #endregion

        #region email messages

        public List<EmailMessage> GetEmailMessageList(int surveyId, int campaignId)
        {
            return GetData<EmailMessage>("survey/" + surveyId + "/surveycampaign/" + campaignId + "/emailmessage", true, true);
        }

        public bool UpdateEmailMessage(int surveyId, int campaignId, EmailMessage emailMessage)
        {
            var url =
                new StringBuilder("survey/" + surveyId + "/surveycampaign/" + campaignId + "/emailmessage/" +
                                  emailMessage.id + "?_method=POST");

            if (emailMessage.from != null)
            {
                if (emailMessage.from.name != null)
                {
                    url.Append("&from[name]=" + Uri.EscapeDataString(emailMessage.from.name));
                }

                if (emailMessage.from.email != null)
                {
                    url.Append("&from[email]=" + Uri.EscapeDataString(emailMessage.from.email));
                }
            }

            // TODO: allow updating of the rest of the properties of an email message

            var results = GetData<Result>(url.ToString(), nonQuery: true);
            return ResultOk(results);
        }

        #endregion

        #region contacts
        // TODO: change this method to take a Contact object
        //public int CreateContact(int surveyId, int campaignId, Contact contact)
        public int CreateContact(int surveyId, int campaignId, string emailAddress = null,
            string firstName = null, string lastName = null, string organization = null, params string[] customFields)
        {
            var url = BuildCreateOrUpdateContactUrl(surveyId, campaignId, null, emailAddress, firstName, lastName, organization, customFields);
            var results = GetData<Result>(url, nonQuery: true);
            if (results == null || results.Count < 1 || results[0].result_ok == false) return -1;
            return results[0].id;
        }

        // TODO: change this method to take a Contact object
        //public int UpdateContact(int surveyId, int campaignId, Contact contact)
        public bool UpdateContact(int surveyId, int campaignId, int contactId, string emailAddress = null,
            string firstName = null, string lastName = null, string organization = null, params string[] customFields)
        {
            var url = BuildCreateOrUpdateContactUrl(surveyId, campaignId, contactId, emailAddress, firstName, lastName,
                organization, customFields);
            var results = GetData<Result>(url, nonQuery: true);
            return ResultOk(results);
        }

        private string BuildCreateOrUpdateContactUrl(int surveyId, int campaignId, int? contactId, string emailAddress = null, string firstName = null, string lastName = null, string organization = null, params string[] customFields)
        {
            var method = contactId == null ? "PUT" : "POST";
            var strContactId = contactId == null ? "" : contactId.ToString();

            var url =
                BuildUrl(
                    "survey/" + surveyId + "/surveycampaign/" + campaignId + "/contact/" + strContactId + "?_method=" +
                    method,
                    new Dictionary<string, string>()
                    {
                        {"semailaddress", emailAddress},
                        {"sfirstname", firstName},
                        {"slastname", lastName},
                        {"sorganization", organization}
                    });

            for (var i = 0; i < customFields.Length; i++)
            {
                if (customFields[i] != null)
                {
                    url.Append("&scustomfield" + (i + 1) + "=" + Uri.EscapeDataString(customFields[i]));
                }
            }

            return url.ToString();
        }

        public bool DeleteContact(int surveyId, int campaignId, int contactId)
        {
            var results = GetData<Result>("survey/" + surveyId + "/surveycampaign/" + campaignId + "/contact/" + contactId +
                      "?_method=DELETE", nonQuery: true);
            return ResultOk(results);
        }
        #endregion

        #region contact lists
        public List<Contact> GetCampaignContactList(int surveyId, int campaignId)
        {
            return GetData<Contact>("survey/" + surveyId + "/surveycampaign/" + campaignId + "/contact", true, true);
        }

        public bool UpdateContactList(int contactListId, string email, string firstName, string lastName, string organization, Dictionary<string, string> customFields)
        {
            var url =
                BuildUrl("contactlist/" + contactListId + "?_method=POST&semailaddress=" + Uri.EscapeDataString(email),
                    new Dictionary<string, string>()
                    {
                        {"sfirstname", firstName},
                        {"slastname", lastName},
                        {"sorganization", organization}
                    });

            foreach (var key in customFields.Keys)
            {
                if (customFields[key] == null) continue;
                url.Append("&custom[" + key + "]=" + Uri.EscapeDataString(customFields[key]));
            }

            var results = GetData<Result>(url.ToString(), nonQuery: true);
            return ResultOk(results);
        }

        #endregion

        private bool ResultOk(List<Result> results)
        {
            // might want to return the result object instead of a bool
            return results.Count > 0 && results[0].result_ok;
        }

        private StringBuilder BuildUrl(string baseUrl, Dictionary<string, string> parameters)
        {
            var url = new StringBuilder(baseUrl);

            foreach (var parameter in parameters.Where(parameter => parameter.Key != null && parameter.Value != null))
            {
                url.Append("&" + parameter.Key + "=" + Uri.EscapeDataString(parameter.Value));
            }

            return url;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="url"></param>
        /// <param name="getAllPages"></param>
        /// <param name="paged"></param>
        /// <param name="nonQuery">If this parameter is true, the method returns a list with just one Result object that will indicate success/failure of the API call.</param>
        /// <returns></returns>
        private List<T> GetData<T>(string url, bool getAllPages = false, bool paged = false, bool nonQuery = false)
        {
            // TODO: use async?
            const int totalRetries = 10;
            const string placeholder = "[removed]";

            var data = new List<T>();
            var delimiter = url.Contains("?") ? "&" : "?";
            var baseUrl = $"{BaseServiceUrl}{url}{delimiter}api_token={ApiToken}&api_token_secret={ApiTokenSecret}";
            var currentUrl = baseUrl;

            var policy = Policy
                .Handle<WebException>()
                .Retry(totalRetries, (ex, i) =>
                {
                    var exception = ex as WebException;
                    if (exception != null)
                    {
                        SetNLogContextItems(exception, currentUrl);
                        Logger.Log(LogLevel.Error, exception, $"{nameof(WebException)} caught. Retrying {i}/{totalRetries}.");
                    }

                    if (i > 10) throw new Exception($"Total retries exceeded: {totalRetries}", ex);
                });

            if (!paged)
            {
                policy.Execute(() =>
                {
                    if (nonQuery)
                    {
                        var nonQueryResult = ThrottledWebRequest.GetJsonObject<T>(baseUrl);
                        data.Add(nonQueryResult);
                    }
                    else
                    {
                        var queryResult = ThrottledWebRequest.GetJsonObject<Result<T>>(baseUrl);
                        data.Add(queryResult.Data);
                    }
                });
            }
            else
            {
                var page = 1;
                var totalPages = 1;
                if (BatchSize != null && BatchSize > 0) baseUrl += $"&resultsperpage={BatchSize}";

                do
                {
                    // TODO: Conditionally add info messages for 25%, 50%, 75% paging?
                    policy.Execute(() =>
                    {
                        var pagedUrl = $"{baseUrl}&page={page}";
                        currentUrl = pagedUrl;

                        var result = ThrottledWebRequest.GetJsonObject<PagedResult<T>>(pagedUrl);

                        if (!result.result_ok || result.Data == null)
                        {
                            var exMsg = !result.result_ok
                                ? "SurveyGizmo responded with 'result_ok' equal to false, indicating a problem with their system"
                                : "Empty response received from SurveyGizmo";
                            var ex = new WebException(exMsg, WebExceptionStatus.UnknownError);
                            var scrubbedUrl = pagedUrl
                                .Replace(ApiToken, placeholder)
                                .Replace(ApiTokenSecret, placeholder);
                            ex.Data.Add("Url", scrubbedUrl);

                            throw ex;
                        }

                        // Total pages returned on first call
                        if (getAllPages && result.total_pages > 0) totalPages = result.total_pages;

                        data.AddRange(result.Data.Where(d => d != null));
                        page++;
                    });
                } while (page <= totalPages);

                if (page > 1 && page - 1 != totalPages) Logger.Log(LogLevel.Warn, $"Only {page - 1}/{totalPages} pages retrieved!\tUrl: {BaseServiceUrl}{url}");
            }
            return data;
        }

        private static int? GetStatusCode(WebException webException)
        {
            if (webException.Status != WebExceptionStatus.ProtocolError) return null;

            var response = webException.Response as HttpWebResponse;
            if (response != null)
            {
                return (int)response.StatusCode;
            }

            return null;
        }

        private void SetNLogContextItems(WebException webException, string url)
        {
            GlobalDiagnosticsContext.Set("apiUrl", url);
            GlobalDiagnosticsContext.Set("httpStatusCode", GetStatusCode(webException).ToString());
        }
    }
}
