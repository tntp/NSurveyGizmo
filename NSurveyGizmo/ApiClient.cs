using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using Newtonsoft.Json;
using NLog;
using NSurveyGizmo.Models;
using Polly;
using Contact              = NSurveyGizmo.Models.Contact;
using EmailMessage         = NSurveyGizmo.Models.EmailMessage;
using LocalizableString    = NSurveyGizmo.Models.LocalizableString;
using QuestionProperties   = NSurveyGizmo.Models.QuestionProperties;
using Result               = NSurveyGizmo.Models.Result;
using Survey               = NSurveyGizmo.Models.Survey;
using SurveyCampaign       = NSurveyGizmo.Models.SurveyCampaign;
using SurveyQuestion       = NSurveyGizmo.Models.SurveyQuestion;
using SurveyQuestionOption = NSurveyGizmo.Models.SurveyQuestionOption;

namespace NSurveyGizmo
{
    public class ApiClient
    {
        public int? BatchSize = null;
        public string BaseServiceUrl = "https://restapi.surveygizmo.com/v5/";
        public string ApiToken { get; set; }
        public string ApiTokenSecret { get; set; }

        public IThrottledWebRequest ThrottledWebRequest = new ThrottledWebRequest();
        private readonly ILogger _logger;

        public ApiClient()
        {
            _logger = LogManager.GetCurrentClassLogger();
        }

        public ApiClient(ILogger logger)
        {
            _logger = logger;
        }

        #region questions

        public List<SurveyQuestion> GetQuestions(int surveyId, bool getAllPages = true)
        {
            var results = GetData<SurveyQuestion>($"survey/{surveyId}/surveyquestion", getAllPages, true);

            var subQuesitonList = new List<SurveyQuestion>();
            foreach (var result in results)
            {
                if (result.sub_questions != null)
                {
                    subQuesitonList.AddRange(result.sub_questions);
                }
            }
            var subQuesitonListIds = subQuesitonList.Select(i => i.id).ToList();
            return results.Where(i => !subQuesitonListIds.Contains(i.id)).ToList();
        }
        public SurveyQuestion CreateQuestion(int surveyId, int surveyPage, string type, LocalizableString title, string shortName, QuestionProperties props)
        {
            var url = new StringBuilder($"survey/{surveyId}/surveypage/{surveyPage}/surveyquestion?_method=PUT");
            return CreateQuestion(url, type, title, shortName, props);
        }
        
        public SurveyQuestion CreateQuestionRow(int surveyId, int surveyPage, int parentQuestionId, string type, LocalizableString title, string shortName, QuestionProperties props)
        {
            var url = new StringBuilder($"survey/{surveyId}/surveypage/{surveyPage}/surveyquestion/{parentQuestionId}?_method=PUT");
            return CreateQuestion(url, type, title, shortName, props);
        }

        private SurveyQuestion CreateQuestion(StringBuilder url,string type, LocalizableString title, string shortName, QuestionProperties props)
        {
            if (!string.IsNullOrEmpty(shortName))
            {
                url.Append($"&shortname={Uri.EscapeDataString(shortName)}");
            }
            if (!string.IsNullOrEmpty(type))
            {
                url.Append($"&type={Uri.EscapeDataString(type)}");
            }
            if (!string.IsNullOrEmpty(title.English))
            {
                url.Append($"&title={Uri.EscapeDataString(title.English)}");
            }
            if (props != null)
            {
                url.Append($"&properties[required]={props.required}");
                url.Append($"&properties[hidden]={props.hidden}");
                url.Append($"&properties[option_sort]={props.option_sort}");
                url.Append($"&properties[orientation]={props.orientation}");
                url.Append($"&properties[question_description][English]={Uri.EscapeDataString(props.question_description.English)}");
            }
            var response = GetData<SurveyQuestion>(url.ToString());
            return response != null && response.Count > 0 ? response[0] : null;
        }

        #endregion

        #region Question Options
        public List<SurveyQuestionOption> GetQuestionOptions(int surveyId, int questionId, bool getAllPages = true)
        {
            return GetData<SurveyQuestionOption>($"survey/{surveyId}/surveyquestion/{questionId}/surveyoption", getAllPages, true);
        }
        public SurveyQuestionOption GetQuestionOption(int surveyId, int questionId, int optionId, bool getAllPages = true)
        {
            var response = GetData<SurveyQuestionOption>($"survey/{surveyId}/surveyquestion/{questionId}/surveyoption/{optionId}", getAllPages);
            return response != null && response.Count > 0 ? response[0] : null;
        }
        public int CreateQuestionOption(int surveyId, int surveyPage, int questionId, int? orderAfterId, LocalizableString title, string value)
        {
            if (string.IsNullOrEmpty(title.English) || string.IsNullOrEmpty(value))
            {
                return -1;
            }
            var url = new StringBuilder($"survey/{surveyId}/surveypage/{surveyPage}/surveyquestion/{questionId}/surveyoption?_method=PUT");
            
            url.Append($"&title={Uri.EscapeDataString(title.English)}");
            url.Append($"&value={Uri.EscapeDataString(value)}");

            if (orderAfterId != null)
            {
                url.Append($"&after={orderAfterId}");
            }
            var response = GetData<Result>(url.ToString());
            return response != null && response.Count > 0 ? response[0].id : -1;
        }

        public bool DeleteSurveyOption(int surveyId, int optionId, int questionId)
        {
            var url = new StringBuilder($"survey/{surveyId}/surveyquestion/{questionId}/surveyoption/{optionId}?_method=DELETE");
            var response = GetData<Result>(url.ToString(), nonQuery: true);
            return ResultOk(response);
        }

        #endregion

        #region responses

        public List<SurveyResponse> GetResponses(int surveyId, bool getAllPages = true)
        {
            return GetData<SurveyResponse>("survey/" + surveyId + "/surveyresponse", getAllPages, true);
        }
        public SurveyResponse GetResponse(int surveyId, string surveyresponse)
        {
            var response = GetData<SurveyResponse>($"survey/{surveyId}/surveyresponse/{Convert.ToInt32(surveyresponse)}");
            return response != null && response.Count > 0 ? response[0] : null;
        }
        public SurveyResponse CreateSurveyResponse(int surveyId, string status, List<SurveyResponseQuestionData> questionData)
        {
            var url = new StringBuilder($"survey/{surveyId}/surveyresponse?_method=PUT");
            foreach (var sd in questionData)
            {
              var responseFormatted = FormatSurveyQuestionData(sd.questionId, sd.questionShortName, sd.questionOptionIdentifier, sd.value, sd.isResponseAComment, sd.questionOptionTitle);
              url.Append(responseFormatted);
            }
            var response = GetData<SurveyResponse>(url.ToString());
            return response != null && response.Count > 0 ? response[0] : null;
        }
        public StringBuilder FormatSurveyQuestionData(int? questionId, string questionShortname, int? questionOptionIdentifier, string value, bool isResponseComment, string questionOptionTitle)
        {
            var url = new StringBuilder();
            if (questionId != null && questionOptionIdentifier != null && !string.IsNullOrEmpty(value))
            {
                url.Append($"&data[{questionId}][{questionOptionIdentifier}]={Uri.EscapeDataString(value)}");

            }
            else if (questionId != null && !string.IsNullOrEmpty(value) && !isResponseComment)
            {
                url.Append($"&data[{questionId}][value]={Uri.EscapeDataString(value)}");

            }
            else if (questionId != null && !string.IsNullOrEmpty(value) && isResponseComment)
            {
                url.Append($"&data[{questionId}][comment]={Uri.EscapeDataString(value)}");
            }
            if (!string.IsNullOrEmpty(questionShortname) && questionOptionIdentifier != null && !string.IsNullOrEmpty(value))
            {
                url.Append($"&data[{Uri.EscapeDataString(questionShortname)}][{questionOptionIdentifier}]={Uri.EscapeDataString(value)}");

            }
            else if (!string.IsNullOrEmpty(questionShortname) && !string.IsNullOrEmpty(value) && !url.ToString().Contains("data"))
            {
                url.Append($"&data[{Uri.EscapeDataString(questionShortname)}][value={Uri.EscapeDataString(value)}]");
            }
            return url;
        }
        
        #endregion

        #region surveys
        public List<Survey> GetAllSurveys(bool getAllPages = true)
        {
            return GetData<Survey>("survey", getAllPages, true);
        }

        public int CreateSurvey(string title)
        {
            var response = GetData<Survey>($"survey/?_method=PUT&type=survey&title={Uri.EscapeDataString(title ?? "")}");
            // TODO: return the survey object?
            return response != null && response.Count > 0 ? response[0].id : -1;
        }

        public bool DeleteSurvey(int surveyId)
        {
            var results = GetData<Result>($"survey/{surveyId}?_method=DELETE", nonQuery: true);
            return ResultOk(results);
        }

        public Survey GetSurvey(int id)
        {
            var results = GetData<Survey>($"survey/{id}");
            return results != null && results.Count > 0 ? results[0] : null;
        }
        public int CopySurvey(int copyId, string name)
        {
            var response = GetData<Survey>($"survey/{copyId}?_method=POST&copy=true&title={Uri.EscapeDataString(name ?? "")}");
            return response != null && response.Count > 0 ? response[0].id : -1;
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

            var campaigns = GetData<SurveyCampaign>(
                $"survey/{surveyId}/surveycampaign{id}?_method={method}{type}&name={Uri.EscapeDataString(campaignName ?? "")}{copy}");

            // TODO: return campaign object?

            if (campaigns == null || campaigns.Count < 1) return 0;
            return campaigns[0].id;
        }

        public List<SurveyCampaign> GetCampaigns(int surveyId, bool getAllPages = true)
        {
            return GetData<SurveyCampaign>($"survey/{surveyId}/surveycampaign", getAllPages, true);
        }

        public SurveyCampaign GetCampaign(int surveyId, int campaignId)
        {
            var results = GetData<SurveyCampaign>($"survey/{surveyId}/surveycampaign/{campaignId}");
            return results != null && results.Count > 0 ? results[0] : null;
        }

        public bool DeleteCampaign(int surveyId, int campaignId)
        {
            var results = GetData<Result>($"survey/{surveyId}/surveycampaign/{campaignId}?_method=DELETE", nonQuery: true);
            return ResultOk(results);
        }

        public bool UpdateCampaign(int surveyId, SurveyCampaign campaign)
        {
            var url = BuildUrl($"survey/{surveyId}/surveycampaign/{campaign.id}?_method=POST",
                new Dictionary<string, string>() { { "name", campaign.name }, { "status", campaign.status } });

            // TODO: allow updating the rest of the properties of a campaign

            var results = GetData<Result>(url.ToString(), nonQuery: true);

            return ResultOk(results);
        }
        public bool UpdateQcodeOfSurveyQuestion(int surveyId, int questionId, string qCode)
        {
            var url = new StringBuilder();
            var qtype = GetQuestions(surveyId).Where(i => i.id == questionId).Select(q => q._subtype).First();

            if (qtype.ToLower() == "textbox" || qtype.ToLower() == "essay")
            {
                url.Append($"survey/{surveyId}/surveyquestion/{questionId}?_method=POST");
                url.Append($"&varname[]={Uri.EscapeDataString(qCode)}");
            }
            else
            {
                url.Append($"survey/{surveyId}/surveyquestion/{questionId}?_method=POST");
                url.Append($"&varname={Uri.EscapeDataString(qCode)}");
            }

            var results = GetData<Result>(url.ToString(), nonQuery: true);
            return ResultOk(results);
        }

        public bool UpdateQcodeOfSurveyQuestion(int surveyId, Dictionary<int, string> qCodes, int masterQuesitonId)
        {
            var url = new StringBuilder();

            var masterQuestion = GetQuestions(surveyId).FirstOrDefault(x => x.id == masterQuesitonId);
            if (masterQuestion == null) return false;

            url.Append($"survey/{surveyId}/surveyquestion/{masterQuesitonId}?_method=POST");
            
            //Create the array based on new qCodes
            foreach (var subQ in qCodes)
            {
                url.Append($"&varname[{subQ.Key}]={Uri.EscapeDataString(subQ.Value)}");
            }

            //This will remove any existing codes and add new ones
            var results = GetData<Result>(url.ToString(), nonQuery: true);
            return ResultOk(results);
        }

        #endregion

        #region email messages

        public List<EmailMessage> GetEmailMessageList(int surveyId, int campaignId)
        {
            return GetData<EmailMessage>($"survey/{surveyId}/surveycampaign/{campaignId}/emailmessage", true, true);
        }

        public bool UpdateEmailMessage(int surveyId, int campaignId, EmailMessage emailMessage)
        {
            var url =
                new StringBuilder($"survey/{surveyId}/surveycampaign/{campaignId}/emailmessage/{emailMessage.id}?_method=POST");

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
        public Contact GetContact(int surveyId, int campaignId, int contactId)
        {
            var url =
                new StringBuilder($"survey/{surveyId}/surveycampaign/{campaignId}/surveycontact/{contactId}");
            var response = GetData<Contact>(url.ToString());
            return response != null && response.Count > 0 ? response[0] : null;
        }
        public int CreateContact(int surveyId, int campaignId, string emailAddress = null,
            string firstName = null, string lastName = null, string organization = null, params string[] customFields)
        {
            var url = BuildCreateOrUpdateContactUrl(surveyId, campaignId, null, emailAddress, firstName, lastName, organization, customFields);
            var response = GetData<Result>(url, nonQuery: true);
            return response != null && response.Count > 0 ? response[0].id : -1;
        }
        public int CreateContact(int surveyId, int campaignId, Contact contact)
        {
            var url = BuildCreateOrUpdateContactUrl(surveyId, campaignId, null, contact);
            var response = GetData<Result>(url, nonQuery:true);
            return response != null && response.Count > 0 ? response[0].id : -1;
        }
        public bool UpdateContact(int surveyId, int campaignId, int contactId, Contact contact)
        {
            var url = BuildCreateOrUpdateContactUrl(surveyId, campaignId, contactId, contact, null);
            var results = GetData<Result>(url, nonQuery: true);
            return ResultOk(results);
        }
        public bool UpdateContact(int surveyId, int campaignId, int contactId, string emailAddress = null,
            string firstName = null, string lastName = null, string organization = null, params string[] customFields)
        {
            var url = BuildCreateOrUpdateContactUrl(surveyId, campaignId, contactId, emailAddress, firstName, lastName,
                organization, customFields);
            var results = GetData<Result>(url, nonQuery: true);
            return ResultOk(results);
        }

        private string BuildCreateOrUpdateContactUrl(int surveyId, int campaignId, int? contactId, Contact contact, params string[] customFields)
        {
            var method = contactId == null ? "PUT" : "POST";
            var strContactId = contactId == null ? "" : contactId.ToString();

            var url =
                BuildUrl(
                    $"survey/{surveyId}/surveycampaign/{campaignId}/surveycontact/{strContactId}?_method={method}",
                    new Dictionary<string, string>()
                    {
                        {"email_address", contact.semailaddress},
                        {"first_name", contact.sfirstname},
                        {"last_name", contact.slastname},
                        {"organization", contact.sorganization}
                    });
            if (customFields != null)
            {
                for (var i = 0; i < customFields.Length; i++)
                {
                    if (customFields[i] != null)
                    {
                        url.Append("&customfield" + (i + 1) + "=" + Uri.EscapeDataString(customFields[i]));
                    }
                }
            }

            return url.ToString();
        }
        private string BuildCreateOrUpdateContactUrl(int surveyId, int campaignId, int? contactId, string emailAddress = null, string firstName = null, string lastName = null, string organization = null, params string[] customFields)
        {
            var method = contactId == null ? "PUT" : "POST";
            var strContactId = contactId == null ? "" : contactId.ToString();

            var url =
                BuildUrl(
                    "survey/" + surveyId + "/surveycampaign/" + campaignId + "/surveycontact/" + strContactId + "?_method=" +
                    method,
                    new Dictionary<string, string>()
                    {
                        {"email_address", emailAddress},
                        {"first_name", firstName},
                        {"last_name", lastName},
                        {"organization", organization}
                    });
            if (customFields != null)
            {
                for (var i = 0; i < customFields.Length; i++)
                {
                    if (customFields[i] != null)
                    {
                        url.Append("&customfield" + (i + 1) + "=" + Uri.EscapeDataString(customFields[i]));
                    }
                }
            }

            return url.ToString();
        }


        public bool DeleteContact(int surveyId, int campaignId, int contactId)
        {
            var results = GetData<Result>($"survey/{surveyId}/surveycampaign/{campaignId}/surveycontact/{contactId}?_method=DELETE", nonQuery: true);
            return ResultOk(results);
        }
        #endregion

        #region contact lists
        public List<Contact> GetCampaignContactList(int surveyId, int campaignId)
        {
            return GetData<Contact>($"survey/{surveyId}/surveycampaign/{campaignId}/surveycontact", true, true);
        }
        public bool UpdateContactList(int contactListId, string email, string firstName, string lastName, string organization, Dictionary<string, string> customFields)
        {
            var url =
                BuildUrl("contactlist/" + contactListId + "/contactlistcontact?_method=PUT&email_address=" + Uri.EscapeDataString(email),
                    new Dictionary<string, string>()
                    {
                        {"first_name", firstName},
                        {"last_name", lastName},
                        {"organization", organization}
                    });
            if (customFields != null)
            {
                foreach (var key in customFields.Keys)
                {
                    if (customFields[key] == null) continue;
                    url.Append("&custom[" + key + "]=" + Uri.EscapeDataString(customFields[key]));
                }
            }
            var results = GetData<Result>(url.ToString(), nonQuery: true);
            return ResultOk(results);
        }
        public int CreateContactList(string listName)
        {
            var response = GetData<Result>($"contactlist?_method=PUT&list_name={listName}" );
            return response != null && response.Count > 0 ? response[0].id : -1;
        }
        public int GetContactList(int listId)
        {
            var response = GetData<Result>($"contactlist/{listId}");
            return response != null && response.Count > 0 ? response[0].id : -1;
        }
        public List<Contact> GetAllContactsForContactList(int listId)
        {
            return GetData<Contact>($"contactlist/{listId}/contactlistcontact", true, true);
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

            var data = new List<T>();
            var delimiter = url.Contains("?") ? "&" : "?";
            var baseUrl = $"{BaseServiceUrl}{url}{delimiter}api_token={ApiToken}&api_token_secret={ApiTokenSecret}";
            var currentUrl = baseUrl;

            var policy = Policy
                .Handle<WebException>()
                .Retry(totalRetries, (ex, i) =>
                {
                    var webException = ex as WebException;
                    if (webException?.Status == WebExceptionStatus.ProtocolError &&
                        webException.Response is HttpWebResponse response)
                    {
                        GlobalDiagnosticsContext.Set("httpStatusCode", response.StatusCode.ToString());
                    }
                    GlobalDiagnosticsContext.Set("apiUrl", GetScrubbedUrl(currentUrl));

                    _logger.Log(LogLevel.Error, ex, $"{ex.Message} - Attempt: {i}/{totalRetries}");
                });

            if (!paged)
            {
                policy.Execute(() =>
                {
                    if (nonQuery)
                    {
                        try
                        {
                            var nonQueryResult = ThrottledWebRequest.GetJsonObject<T>(baseUrl);
                            data.Add(nonQueryResult);
                        }
                        catch (WebException e)
                        {
                            var resp = e.Response as HttpWebResponse;
                            using (var sr = new StreamReader(resp.GetResponseStream()))
                            {
                                var result = sr.ReadToEnd();
                                var exMsg = JsonConvert.DeserializeObject<Result>(result).message;
                                var ex = new WebException(exMsg, WebExceptionStatus.UnknownError);
                                ex.Data.Add("Url", GetScrubbedUrl(baseUrl));

                                throw ex;
                            }
                        }
                    }
                    else
                    {
                        var queryResult = ThrottledWebRequest.GetJsonObject<Result<T>>(baseUrl);
                        if (queryResult.Data != null)
                        {
                            data.Add(queryResult.Data);
                        }
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
                            ex.Data.Add("Url", GetScrubbedUrl(pagedUrl));

                            throw ex;
                        }

                        // Total pages returned on first call
                        if (getAllPages && result.total_pages > 0) totalPages = result.total_pages;

                        data.AddRange(result.Data.Where(d => d != null));
                        page++;
                    });
                } while (page <= totalPages);

                if (page > 1 && page - 1 != totalPages) _logger.Log(LogLevel.Warn, $"Only {page - 1}/{totalPages} pages retrieved!\tUrl: {BaseServiceUrl}{url}");
            }
            return data;
        }

        private string GetScrubbedUrl(string url)
        {
            const string placeholder = "[removed]";
            return url.Replace(ApiToken, placeholder).Replace(ApiTokenSecret, placeholder);
        }
    }
}
