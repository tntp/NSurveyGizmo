using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NSurveyGizmo.Tests
{
    [TestClass]
    public class IntegrationTests
    {
        private static readonly Regex HtmlRegex = new Regex("<.*?>", RegexOptions.Compiled);

        [TestMethod]
        public void testquestions()
        {
            var creds = File.ReadAllLines(@"C:\tmp\sg_creds.txt");
            var apiClient = new ApiClient() { ApiToken = creds[0], ApiTokenSecret = creds[1] };

            Func<QuestionOptions[], string> joinOptions = (options) => options != null && options.Length > 0
                ? "\noptions :\n" + string.Join(",\n\n", options.Select(o => o.title.English)) +
                  ",\n"
                : "";

            var questions = apiClient.GetQuestions(2687802).Where(q => q._type == "SurveyQuestion").ToList();

            var gizmoQuestions = questions.Select(q => new 
            {
                ID = q.id.ToString(),
                Question = q.title != null ? q.title.English + joinOptions(q.options) : "",
                QCode =
                    q.properties != null && q.properties.question_description != null &&
                    q.properties.question_description.English != null
                        ? HtmlRegex.Replace(q.properties.question_description.English, string.Empty)
                        : "",
                AnswerFormat = q._subtype ?? ""
            }).ToList();



            foreach (var gizmoQuestion in gizmoQuestions)
            {
                Trace.WriteLine(gizmoQuestion.QCode);
            }
        }

        [TestMethod]
        public void Create_And_Delete_Survey_And_Campaign_Json()
        {
            var testStartedAt = DateTime.Now;

            var creds = File.ReadAllLines(@"C:\tmp\sg_creds.txt");
            var apiClient = new ApiClient() { ApiToken = creds[0], ApiTokenSecret = creds[1] };

            // create survey
            var title = "Test Survey " + testStartedAt;
            var surveyId = apiClient.CreateSurvey(title);
            Assert.IsTrue(surveyId > 0);

            // verify that the survey is returned in the list of all surveys
            var allSurveys = apiClient.GetAllSurveys();
            Assert.IsTrue(allSurveys.Any(s => s.title == title));

            // get survey
            var survey = apiClient.GetSurvey(surveyId);
            Assert.IsNotNull(survey);
            Assert.AreEqual(surveyId, survey.id);
            Assert.AreEqual(title, survey.title);
            Assert.AreEqual("Launched", survey.status);

            // create "master" campaign
            var masterCampaignName = "Master";
            var masterCampaignId = apiClient.CreateCampaign(surveyId, masterCampaignName);
            Assert.IsTrue(masterCampaignId > 0);

            // create campaign that is a copy of the "master" campaign
            var campaignName = "Campaign " + testStartedAt;
            var campaignId = apiClient.CreateCampaign(surveyId, campaignName, masterCampaignId);
            Assert.IsTrue(campaignId > 0);

            // check if the campaign is returned in the list of all campaigns
            var campaigns = apiClient.GetCampaigns(surveyId);
            Assert.IsNotNull(campaigns);
            campaigns = campaigns.Where(c => c._subtype == "email").ToList();
            // 2 email campaigns expected
            Assert.AreEqual(2, campaigns.Count);
            Assert.IsTrue(campaigns.Any(c => c.status == "Active" && c.name == campaignName && c.id == campaignId));

            // get the campaign
            var campaign = apiClient.GetCampaign(surveyId, campaignId);
            Assert.IsNotNull(campaign);
            Assert.AreEqual(campaignName, campaign.name);
            Assert.AreEqual("Active", campaign.status);
            Assert.AreEqual(campaignId, campaign.id);

            // get the email messages for the campaign
            var emailMessages = apiClient.GetEmailMessageList(surveyId, campaignId);
            Assert.IsNotNull(emailMessages);
            Assert.AreEqual(1, emailMessages.Count);
            Assert.IsNotNull(emailMessages[0]);
            Assert.IsNotNull(emailMessages[0].from);
            Assert.IsNotNull(emailMessages[0].from.name);
            // Survey Research is the default value for the "From" name
            Assert.AreEqual("Survey Research", emailMessages[0].from.name);

            // update the "From" name on the email message
            var updatedFromName = "Updated " + testStartedAt;
            emailMessages[0].from.name = updatedFromName;
            var nameWasUpdated = apiClient.UpdateEmailMessage(surveyId, campaignId, emailMessages[0]);
            Assert.IsTrue(nameWasUpdated);

            // get the campaign again to verify that the name was actually updated
            emailMessages = apiClient.GetEmailMessageList(surveyId, campaignId);
            Assert.IsNotNull(emailMessages);
            Assert.AreEqual(1, emailMessages.Count);
            Assert.IsNotNull(emailMessages[0]);
            Assert.IsNotNull(emailMessages[0].from);
            Assert.IsNotNull(emailMessages[0].from.name);
            Assert.AreEqual(updatedFromName, emailMessages[0].from.name);

            // update the campaign name
            var updatedCampaignName = campaignName + " Updated";
            campaign.name = updatedCampaignName;
            var campaignNameUpdatedSuccess = apiClient.UpdateCampaign(surveyId, campaign);
            Assert.IsTrue(campaignNameUpdatedSuccess);

            // get the campaign again to verify that the name was updated
            campaign = apiClient.GetCampaign(surveyId, campaignId);
            Assert.AreEqual(updatedCampaignName, campaign.name);


            // create contact
            var datetime = testStartedAt.ToString("yyyyMMddHHmmss");
            var contactId = apiClient.CreateContact(surveyId, campaignId, "test_" + datetime + "@tntp.org", "John", "Doe", "Test Organization");
            Assert.IsTrue(contactId > 0);

            // verify that the contact is in the list of contacts
            var campaignContactList = apiClient.GetCampaignContactList(surveyId, campaignId);
            Assert.AreEqual(1, campaignContactList.Count);
            Assert.IsTrue(campaignContactList.Any(c => c.id == contactId));

            // update contact
            var updated = apiClient.UpdateContact(surveyId, campaignId, contactId, null, null, "Smith");
            Assert.IsTrue(updated);

            // delete contact
            var deleted = apiClient.DeleteContact(surveyId, campaignId, contactId);
            Assert.IsTrue(deleted);

            // delete master campaign
            var campaignDeleted = apiClient.DeleteCampaign(surveyId, masterCampaignId);
            Assert.IsTrue(campaignDeleted);

            // delete campaign
            campaignDeleted = apiClient.DeleteCampaign(surveyId, campaignId);
            Assert.IsTrue(campaignDeleted);

            // delete survey
            var surveyDeleted = apiClient.DeleteSurvey(surveyId);
            Assert.IsTrue(surveyDeleted);
        }
    }
}
