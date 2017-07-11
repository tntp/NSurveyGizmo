using NSurveyGizmo.Models;
using System;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NSurveyGizmo.Tests
{
    [TestClass]
    public partial class IntegrationTests
    {
        private static readonly Regex HtmlRegex = new Regex("<.*?>", RegexOptions.Compiled);

        [TestMethod]
        public void TestQuestions()
        {
            Func<QuestionOptions[], string> joinOptions = options => options != null && options.Length > 0
                ? "\noptions :\n" + string.Join(",\n\n", options.Select(o => o.title)) +
                  ",\n"
                : "";

            var questions = apiClient.GetQuestions(2687802).Where(q => q._type == "SurveyQuestion").ToList();

            var gizmoQuestions = questions.Select(q => new
            {
                ID = q.id.ToString(),
                Question = q.title != null ? q.title + joinOptions(q.options) : "",
                QCode =
                    q.properties?.question_description != null
                        ? HtmlRegex.Replace(q.properties.question_description.English, string.Empty)
                        : "",
                AnswerFormat = q._type ?? ""
            }).ToList();



            foreach (var gizmoQuestion in gizmoQuestions)
            {
                Trace.WriteLine(gizmoQuestion.QCode);
            }
        }

        [TestMethod]
        public void Create_And_Delete_Survey_And_Campaign_Json()
        {
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
            var fakeContact = new Contact
            {
                emailAddress = "U_test12345@tntp.org",
                firstName = "John",
                lastName = "Doe",
                organization = "Test Organization"
            };
            var contactId = apiClient.CreateContact(surveyId, campaignId, fakeContact);
            Assert.IsTrue(contactId > 0);
           
            // verify that the contact is in the list of contacts
            var campaignContactList = apiClient.GetCampaignContactList(surveyId, campaignId);
            Assert.AreEqual(1, campaignContactList.Count);
            Assert.IsTrue(campaignContactList.Any(c => c.id == contactId));

            // update contact
            fakeContact.lastName = "Smith";
            var updated = apiClient.UpdateContact(surveyId, campaignId, Convert.ToInt32(contactId), fakeContact);
            Assert.IsTrue(updated);

            // delete contact
            var deleted = apiClient.DeleteContact(surveyId, campaignId, Convert.ToInt32(contactId));
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

        [TestMethod()]
        public void Create_Survey_Test()
        {
            // create survey
            var title = "Test Survey " + testStartedAt;
            var surveyId = apiClient.CreateSurvey(title);
            Assert.IsTrue(surveyId > 0);

            // get survey
            var survey = apiClient.GetSurvey(surveyId);
            Assert.IsNotNull(survey);
            Assert.AreEqual(surveyId, survey.id);
            Assert.AreEqual(title, survey.title);
            Assert.AreEqual("Launched", survey.status);
        }

        [TestMethod]
        public void Create_Questions_Test()
        {
            // create survey
            var title = "Test Survey " + testStartedAt;
            var surveyId = apiClient.CreateSurvey(title);
            Assert.IsTrue(surveyId > 0);

            // get survey
            var survey = apiClient.GetSurvey(surveyId);
            Assert.IsNotNull(survey);
            Assert.AreEqual(surveyId, survey.id);
            Assert.AreEqual(title, survey.title);
            Assert.AreEqual("Launched", survey.status);

            // create questions
            var firstQuestionTitle = new LocalizableString("Test survey question");
            var secondQuestionTitle = new LocalizableString("Test survey question2");
           
            var q1 = apiClient.CreateQuestion(surveyId, 1, "checkbox", firstQuestionTitle, null, null);
            var q2 = apiClient.CreateQuestion(surveyId, 1, "essay", secondQuestionTitle, null, null);

            // get questions
            var questions = apiClient.GetQuestions(surveyId);
            Assert.IsNotNull(questions);
            Assert.IsTrue(questions.Any(i => i.id == q1.id));
            Assert.IsTrue(questions.Any(i => i.id == q2.id));
        }

        [TestMethod]
        public void Create_Contact_Test()
        {
            // create survey
            var title = "Test Survey " + testStartedAt;
            var surveyId = apiClient.CreateSurvey(title);
            Assert.IsTrue(surveyId > 0);

            // get survey
            var survey = apiClient.GetSurvey(surveyId);
            Assert.IsNotNull(survey);
            Assert.AreEqual(surveyId, survey.id);
            Assert.AreEqual(title, survey.title);
            Assert.AreEqual("Launched", survey.status);

            //create survey campaign
            var campaign = apiClient.CreateCampaign(surveyId, "testCampaign");
            Assert.AreEqual(campaign, apiClient.GetCampaign(surveyId, campaign).id);

            // create survey contact
            var fakeContact = new Contact
            {
                emailAddress = "U_test12345@tntp.org",
                firstName = "John",
                lastName = "Doe",
                organization = "Test Organization"
            };
            var contactId = apiClient.CreateContact(surveyId, campaign, fakeContact);
            Assert.AreEqual(contactId, apiClient.GetContact(surveyId, campaign, Convert.ToInt32(contactId)).id);
        }

        [TestMethod]
        public void Update_Contacts_Test()
        {
            // create survey
            var title = "Test Survey " + testStartedAt;
            var surveyId = apiClient.CreateSurvey(title);
            Assert.IsTrue(surveyId > 0);

            // get survey
            var survey = apiClient.GetSurvey(surveyId);
            Assert.IsNotNull(survey);
            Assert.AreEqual(surveyId, survey.id);
            Assert.AreEqual(title, survey.title);
            Assert.AreEqual("Launched", survey.status);

            //create survey campaign
            var campaign = apiClient.CreateCampaign(surveyId, "testCampaign");
            Assert.AreEqual(campaign, apiClient.GetCampaign(surveyId, campaign).id);

            // create survey contact
            var fakeContact = new Contact
            {
                emailAddress = "U_test12345@tntp.org",
                firstName = "John",
                lastName = "Doe",
                organization = "Test Organization"
            };
            var contactId = apiClient.CreateContact(surveyId, campaign, fakeContact);
            Assert.AreEqual(contactId, apiClient.GetContact(surveyId, campaign, contactId).id);

            // update survey contact
            fakeContact.lastName = "Smith";
            var updateSuccess = apiClient.UpdateContact(surveyId, campaign, Convert.ToInt32(contactId), fakeContact);
            var updatedContact = apiClient.GetContact(surveyId, campaign, contactId);
            Assert.IsTrue(updateSuccess);
            Assert.AreEqual(updatedContact.lastName, fakeContact.lastName);
        }
    }
}
