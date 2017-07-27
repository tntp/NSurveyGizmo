using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using NLog;
using NSurveyGizmo.Models;
using Polly;
using Contact = NSurveyGizmo.Models.Contact;
using EmailMessage = NSurveyGizmo.Models.EmailMessage;
using LocalizableString = NSurveyGizmo.Models.LocalizableString;
using QuestionProperties = NSurveyGizmo.Models.QuestionProperties;
using Result = NSurveyGizmo.Models.Result;
using Survey = NSurveyGizmo.Models.Survey;
using SurveyCampaign = NSurveyGizmo.Models.SurveyCampaign;
using SurveyQuestion = NSurveyGizmo.Models.SurveyQuestion;
using SurveyQuestionOption = NSurveyGizmo.Models.SurveyQuestionOption;

namespace NSurveyGizmo
{
    public class ApiClient
    {
        public IThrottledWebRequest ThrottledWebRequest = new ThrottledWebRequest();
        public int? BatchSize = null;
        public string BaseServiceUrl = "https://restapi.surveygizmo.com/v5/";
        public string ApiToken { get; set; }
        public string ApiTokenSecret { get; set; }
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        #region questions
        public class TupleList<T1, T2, T3> : List<Tuple<T1, T2, T3>>
        {
            public void Add(T1 item, T2 item2, T3 item3)
            {
                Add(new Tuple<T1, T2, T3>(item, item2, item3));
            }
        }
        public List<SurveyQuestion> GetQuestions(int surveyId, bool getAllPages = true)
        {
            var results = GetData<SurveyQuestion>($"survey/{surveyId}/surveyquestion", getAllPages, true);
            var tableQuestionCodes = new TupleList<int, int, string>();//master question id, sub question id, question code
            foreach (var result in results)
            {
                if (result.sub_questions != null && result.varname != null)
                {
                    foreach (var varname in result.varname)
                    {
                        tableQuestionCodes.Add(result.id, Convert.ToInt32(varname.Key), varname.Value);
                    }
                }
            }
            foreach (var result in results)
            {
                result.properties = new QuestionProperties();
                result.properties.question_description = new LocalizableString();
                foreach(var tup in tableQuestionCodes)
                {
                    if (tup.Item2 == result.id)
                    {
                        result.properties.question_description.English = tup.Item3;
                        result.master_question_id = tup.Item1;
                    }
                        
                }
            }
            return results;
        }
        public SurveyQuestion CreateQuestion(int surveyId, int surveyPage, string type, LocalizableString title, string shortName, QuestionProperties props)
        {
            var url = new StringBuilder($"survey/{surveyId}/surveypage/{surveyPage}/surveyquestion?_method=PUT");

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
            //if (!string.IsNullOrEmpty(status))
            //{
            //    url.Append($"&status={Uri.EscapeDataString(status)}"); //breaks the request for some reason
            //}
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
        public bool UpdateQcodeOfSurveyQuestion(int surveyId, int questionId, string qCode, int? masterQuesitonId = null)
        {
            var url = new StringBuilder();
            if (masterQuesitonId == null)
            {
                url.Append($"survey/{surveyId}/surveyquestion/{questionId}?_method=POST");
                var questionOptions = GetQuestionOptions(surveyId, questionId);
           
                if (questionOptions.Count > 0)
                {
                    foreach (var op in questionOptions)
                    {
                        url.Append($"&varname[{op.id}]={Uri.EscapeDataString(qCode)}");
                    }
                }
                else
                {
                    url.Append($"&varname={Uri.EscapeDataString(qCode)}");
                }
            }
            else
            {
                var masterQuestion = GetQuestions(surveyId).FirstOrDefault(x => x.id == masterQuesitonId);
                url.Append($"survey/{surveyId}/surveyquestion/{masterQuesitonId}?_method=POST");
                if (masterQuestion != null)
                {
                    foreach (var subQ in masterQuestion.varname)
                    {
                        if (Convert.ToInt32(subQ.Key) == questionId)
                        {
                            url.Append($"&varname[{questionId}]={Uri.EscapeDataString(qCode)}");
                        }
                        else
                        {
                            url.Append($"&varname[{subQ.Key}]={Uri.EscapeDataString(subQ.Value)}");
                        }
                    }
                }
            }
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
