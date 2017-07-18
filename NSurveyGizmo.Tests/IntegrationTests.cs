using NSurveyGizmo;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using NSurveyGizmo.Models;
using Contact = NSurveyGizmo.Models.Contact;
using LocalizableString = NSurveyGizmo.Models.LocalizableString;
using QuestionOptions = NSurveyGizmo.Models.QuestionOptions;
using QuestionProperties = NSurveyGizmo.Models.QuestionProperties;
using SurveyQuestionOption = NSurveyGizmo.Models.SurveyQuestionOption;

namespace NSurveyGizmo.Tests
{
    [TestClass]
    public partial class IntegrationTests
    {
        private static readonly Regex HtmlRegex = new Regex("<.*?>", RegexOptions.Compiled);
        [Ignore]
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
            var datetime = testStartedAt.ToString("yyyyMMddHHmmss");
            var fakeContact = new Contact();
            fakeContact.semailaddress = "U_test12345@tntp.org";
            fakeContact.sfirstname = "John";
            fakeContact.slastname = "Doe";
            fakeContact.sorganization = "Test sorganization";
            var contactId = apiClient.CreateContact(surveyId, campaignId, fakeContact);
            Assert.IsTrue(contactId > 0);
           
            // verify that the contact is in the list of contacts
            var campaignContactList = apiClient.GetCampaignContactList(surveyId, campaignId);
            Assert.AreEqual(1, campaignContactList.Count);
            Assert.IsTrue(campaignContactList.Any(c => c.id == contactId));

            // update contact
            fakeContact.slastname = "Smith";
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
        [TestMethod()]
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

            var q1 = apiClient.CreateQuestion(surveyId, 1, "text", firstQuestionTitle, "q1Short", null);
            var q2 = apiClient.CreateQuestion(surveyId, 1, "menu", secondQuestionTitle, "q2Short", null);

            // get questions
            var questions = apiClient.GetQuestions(surveyId);
            Assert.IsNotNull(questions);
            Assert.IsTrue(questions.Any(i => i.id == q1.id));
            Assert.IsTrue(questions.Any(i => i.id == q2.id));
        }
        [TestMethod()]
        public void Create_QuestionOptions_Test()
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
            var q1 = apiClient.CreateQuestion(surveyId, 1, "text", firstQuestionTitle, "q1Short", null);
            var q2 = apiClient.CreateQuestion(surveyId, 1, "menu", secondQuestionTitle, "q2Short", null);

            // get questions
            var questions = apiClient.GetQuestions(surveyId);
            Assert.IsNotNull(questions);
            Assert.IsTrue(questions.Any(i => i.id == q1.id));
            Assert.IsTrue(questions.Any(i => i.id == q2.id));

            // add question options
            int[] opIds = new int[6];
            for (var i = 0; i <= 5; i++)
            {
                var questionOptionTitle = new LocalizableString("option" + i);
                
                var opId = apiClient.CreateQuestionOption(surveyId, 1, q2.id, null, questionOptionTitle, $"option{i}val");
                opIds[i] = opId;
            }

            // get question options
            for (var i = 0; i <= 5; i++)
            {
                var getQuestionOp = apiClient.GetQuestionOption(surveyId, q2.id, opIds[i]);
                Assert.AreEqual(opIds[i], getQuestionOp.id);
            }
            Assert.AreEqual(apiClient.GetQuestionOptions(surveyId, q2.id).Count, 6);

        }
        [TestMethod()]
        public void Delete_QuestionOptions_Test()
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
            var q1 = apiClient.CreateQuestion(surveyId, 1, "text", firstQuestionTitle, "q1Short", null);
            var q2 = apiClient.CreateQuestion(surveyId, 1, "menu", secondQuestionTitle, "q2Short", null);

            // get questions
            var questions = apiClient.GetQuestions(surveyId);
            Assert.IsNotNull(questions);
            Assert.IsTrue(questions.Any(i => i.id == q1.id));
            Assert.IsTrue(questions.Any(i => i.id == q2.id));

            // add question options
            int[] opIds = new int[6];
            for (var i = 0; i <= 5; i++)
            {
                var questionOptionTitle = new LocalizableString("option" + i);

                var opId = apiClient.CreateQuestionOption(surveyId, 1, q2.id, null, questionOptionTitle, $"option{i}val");
                opIds[i] = opId;
            }

            // get question options
            for (var i = 0; i <= 5; i++)
            {
                var getQuestionOp = apiClient.GetQuestionOption(surveyId, q2.id, opIds[i]);
                Assert.AreEqual(opIds[i], getQuestionOp.id);
            }
            Assert.AreEqual(apiClient.GetQuestionOptions(surveyId, q2.id).Count, 6);

            // delete question options
            var deleteSuccess = apiClient.DeleteSurveyOption(surveyId, opIds[1], q2.id);
            Assert.IsTrue(deleteSuccess);
            Assert.AreEqual(apiClient.GetQuestionOptions(surveyId, q2.id).Count, 5);
        }
        [TestMethod()]
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

            // create questions
            var firstQuestionTitle = new LocalizableString("Test survey question");
            var secondQuestionTitle = new LocalizableString("Test survey question2");

            var q1 = apiClient.CreateQuestion(surveyId, 1, "text", firstQuestionTitle, "q1Short", null);
            var q2 = apiClient.CreateQuestion(surveyId, 1, "menu", secondQuestionTitle, "q2Short", null);

            // get questions
            var questions = apiClient.GetQuestions(surveyId);
            Assert.IsNotNull(questions);
            Assert.IsTrue(questions.Any(i => i.id == q1.id));
            Assert.IsTrue(questions.Any(i => i.id == q2.id));

            // add question options
            int[] opIds = new int[6];
            for (var i = 0; i <= 5; i++)
            {
                var questionOptionTitle = new LocalizableString("option" + i);

                var opId = apiClient.CreateQuestionOption(surveyId, 1, q2.id, null, questionOptionTitle, $"option{i}val");
                opIds[i] = opId;
            }

            // get question options
            for (var i = 0; i <= 5; i++)
            {
                var getQuestionOp = apiClient.GetQuestionOption(surveyId, q2.id, opIds[i]);
                Assert.AreEqual(opIds[i], getQuestionOp.id);
            }

            //create survey campaign
            var campaign = apiClient.CreateCampaign(surveyId, "testCampaign");
            Assert.AreEqual(campaign, apiClient.GetCampaign(surveyId, campaign).id);

            // create survey contact
            var fakeContact = new Contact();
            fakeContact.semailaddress = "U_test12345@tntp.org";
            fakeContact.sfirstname = "John";
            fakeContact.slastname = "Doe";
            fakeContact.sorganization = "Test sorganization";
            var fakeContact2 = new Contact();
            fakeContact2.semailaddress = "U_test6789@tntp.org";
            fakeContact2.sfirstname = "Jane";
            fakeContact2.slastname = "Doe";
            fakeContact2.sorganization = "Test sorganization";
            var johnsContactId = apiClient.CreateContact(surveyId, campaign, fakeContact);
            var janesContactId = apiClient.CreateContact(surveyId, campaign, fakeContact2.semailaddress, fakeContact2.sfirstname, fakeContact2.slastname, fakeContact2.sorganization, null);
            var getContact = apiClient.GetContact(surveyId, campaign, johnsContactId);
            var getContact2 = apiClient.GetContact(surveyId, campaign, janesContactId);
            var campaignContact = apiClient.GetCampaignContactList(surveyId, campaign);
            Assert.IsTrue(campaignContact.Any(i => i.id == johnsContactId));
            Assert.AreEqual(johnsContactId, getContact.id);
            Assert.IsTrue(campaignContact.Any(i => i.id == janesContactId));
            Assert.AreEqual(janesContactId, getContact2.id);
        }
        [TestMethod()]
        public void Create_ContactList_Test()
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

            var q1 = apiClient.CreateQuestion(surveyId, 1, "text", firstQuestionTitle, "q1Short", null);
            var q2 = apiClient.CreateQuestion(surveyId, 1, "menu", secondQuestionTitle, "q2Short", null);

            // get questions
            var questions = apiClient.GetQuestions(surveyId);
            Assert.IsNotNull(questions);
            Assert.IsTrue(questions.Any(i => i.id == q1.id));
            Assert.IsTrue(questions.Any(i => i.id == q2.id));

            // add question options
            int[] opIds = new int[6];
            for (var i = 0; i <= 5; i++)
            {
                var questionOptionTitle = new LocalizableString("option" + i);

                var opId = apiClient.CreateQuestionOption(surveyId, 1, q2.id, null, questionOptionTitle, $"option{i}val");
                opIds[i] = opId;
            }

            // get question options
            for (var i = 0; i <= 5; i++)
            {
                var getQuestionOp = apiClient.GetQuestionOption(surveyId, q2.id, opIds[i]);
                Assert.AreEqual(opIds[i], getQuestionOp.id);
            }

            //create survey campaign
            var campaign = apiClient.CreateCampaign(surveyId, "testCampaign");
            Assert.AreEqual(campaign, apiClient.GetCampaign(surveyId, campaign).id);

            // create survey contacts
            var fakeContact = new Contact();
            fakeContact.semailaddress = "U_test12345@tntp.org";
            fakeContact.sfirstname = "John";
            fakeContact.slastname = "Doe";
            fakeContact.sorganization = "Test sorganization";
            var contactId = apiClient.CreateContact(surveyId, campaign, fakeContact);

            var fakeContact2 = new Contact();
            fakeContact2.semailaddress = "U_test4567@tntp.org";
            fakeContact2.sfirstname = "Jane";
            fakeContact2.slastname = "Doe";
            fakeContact2.sorganization = "Test sorganization";
            var contactId2 = apiClient.CreateContact(surveyId, campaign, fakeContact);

            var fakeContact3 = new Contact();
            fakeContact3.semailaddress = "U_test8910@tntp.org";
            fakeContact3.sfirstname = "Frank";
            fakeContact3.slastname = "Sinatra";
            fakeContact3.sorganization = "Test sorganization";
            var contactId3 = apiClient.CreateContact(surveyId, campaign, fakeContact);

            //get survey contact list
            var getContact = apiClient.GetContact(surveyId, campaign, contactId);
            Assert.AreEqual(contactId, getContact.id);
            var getSurveyContactList = apiClient.GetCampaignContactList(surveyId, campaign);
            Assert.AreEqual(getSurveyContactList.Count, 3);
            
        }
        [TestMethod()]
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

            // create questions
            var firstQuestionTitle = new LocalizableString("Test survey question");
            var secondQuestionTitle = new LocalizableString("Test survey question2");

            var q1 = apiClient.CreateQuestion(surveyId, 1, "text", firstQuestionTitle, "q1Short", null);
            var q2 = apiClient.CreateQuestion(surveyId, 1, "menu", secondQuestionTitle, "q2Short", null);

            // get questions
            var questions = apiClient.GetQuestions(surveyId);
            Assert.IsNotNull(questions);
            Assert.IsTrue(questions.Any(i => i.id == q1.id));
            Assert.IsTrue(questions.Any(i => i.id == q2.id));

            // add question options
            int[] opIds = new int[6];
            for (var i = 0; i <= 5; i++)
            {
                var questionOptionTitle = new LocalizableString("option" + i);

                var opId = apiClient.CreateQuestionOption(surveyId, 1, q2.id, null, questionOptionTitle, $"option{i}val");
                opIds[i] = opId;
            }

            // get question options
            for (var i = 0; i <= 5; i++)
            {
                var getQuestionOp = apiClient.GetQuestionOption(surveyId, q2.id, opIds[i]);
                Assert.AreEqual(opIds[i], getQuestionOp.id);
            }

            //create survey campaign
            var campaign = apiClient.CreateCampaign(surveyId, "testCampaign");
            Assert.AreEqual(campaign, apiClient.GetCampaign(surveyId, campaign).id);

            // create survey contact
            var fakeContact = new Contact();
            fakeContact.semailaddress = "U_test12345@tntp.org";
            fakeContact.sfirstname = "John";
            fakeContact.slastname = "Doe";
            fakeContact.sorganization = "Test sorganization";
            var contactId = apiClient.CreateContact(surveyId, campaign, fakeContact);
            Assert.AreEqual(contactId, apiClient.GetContact(surveyId, campaign, contactId).id);

            // update survey contact
            fakeContact.slastname = "Smith";
            var updateSuccess = apiClient.UpdateContact(surveyId, campaign, contactId, fakeContact);
            var updatedContact = apiClient.GetContact(surveyId, campaign, contactId);
            Assert.IsTrue(updateSuccess);
            Assert.AreEqual(updatedContact.slastname, "Smith");
            fakeContact.slastname = "Smithers";
            var updateSuccess2 = apiClient.UpdateContact(surveyId, campaign, contactId, fakeContact.semailaddress, fakeContact.sfirstname, fakeContact.slastname, fakeContact.sorganization, null);
            var updatedContact2 = apiClient.GetContact(surveyId, campaign, contactId);
            Assert.IsTrue(updateSuccess2);
            Assert.AreEqual(updatedContact2.slastname, "Smithers");
        }
        [TestMethod()]
        public void Delete_Contacts_Test()
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

            var q1 = apiClient.CreateQuestion(surveyId, 1, "text", firstQuestionTitle, "q1Short", null);
            var q2 = apiClient.CreateQuestion(surveyId, 1, "menu", secondQuestionTitle, "q2Short", null);

            // get questions
            var questions = apiClient.GetQuestions(surveyId);
            Assert.IsNotNull(questions);
            Assert.IsTrue(questions.Any(i => i.id == q1.id));
            Assert.IsTrue(questions.Any(i => i.id == q2.id));

            // add question options
            int[] opIds = new int[6];
            for (var i = 0; i <= 5; i++)
            {
                var questionOptionTitle = new LocalizableString("option" + i);

                var opId = apiClient.CreateQuestionOption(surveyId, 1, q2.id, null, questionOptionTitle, $"option{i}val");
                opIds[i] = opId;
            }

            // get question options
            for (var i = 0; i <= 5; i++)
            {
                var getQuestionOp = apiClient.GetQuestionOption(surveyId, q2.id, opIds[i]);
                Assert.AreEqual(opIds[i], getQuestionOp.id);
            }
            //create survey campaign
            var campaign = apiClient.CreateCampaign(surveyId, "testCampaign");
            Assert.AreEqual(campaign, apiClient.GetCampaign(surveyId, campaign).id);

            // create survey contacts
            var fakeContact = new Contact();
            fakeContact.semailaddress = "U_test12345@tntp.org";
            fakeContact.sfirstname = "John";
            fakeContact.slastname = "Doe";
            fakeContact.sorganization = "Test sorganization";
            var contactId = apiClient.CreateContact(surveyId, campaign, fakeContact);

            var fakeContact2 = new Contact();
            fakeContact2.semailaddress = "U_test4567@tntp.org";
            fakeContact2.sfirstname = "Jane";
            fakeContact2.slastname = "Doe";
            fakeContact2.sorganization = "Test sorganization";
            var contactId2 = apiClient.CreateContact(surveyId, campaign, fakeContact);

            var fakeContact3 = new Contact();
            fakeContact3.semailaddress = "U_test8910@tntp.org";
            fakeContact3.sfirstname = "Frank";
            fakeContact3.slastname = "Sinatra";
            fakeContact3.sorganization = "Test sorganization";
            var contactId3 = apiClient.CreateContact(surveyId, campaign, fakeContact);

            //get survey contact list
            var getContact = apiClient.GetContact(surveyId, campaign, contactId);
            Assert.AreEqual(contactId, getContact.id);
            var getSurveyContactList = apiClient.GetCampaignContactList(surveyId, campaign);
            Assert.AreEqual(getSurveyContactList.Count, 3);

            //delete contact from list
            var updatedList = apiClient.DeleteContact(surveyId, campaign, contactId3);
            var getUpdatedSurveyContactList = apiClient.GetCampaignContactList(surveyId, campaign);
            Assert.IsTrue(updatedList);
            Assert.AreEqual(getUpdatedSurveyContactList.Count, 2);
        }

        [TestMethod()]
        public void TestDeserialization()
        {
            // Base64 encoded JSON output from SurveyResponse object
            var rawData =
                @"eyJyZXN1bHRfb2siOnRydWUsImRhdGEiOnsgImlkIjoiMSIsImNvbnRhY3RfaWQiOiIiLCJzdGF0dXMiOiJDb21wbGV0ZSIsImlzX3Rlc3RfZGF0YSI6IjAiLCJkYXRlX3N1Ym1pdHRlZCI6IjIwMTctMDctMTQgMTE6Mzc6MjAgRURUIiwic2Vzc2lvbl9pZCI6IjE1MDAwNDY2NDBfNTk2OGU1MzA3M2FiZjguNzM1NzU4MDEiLCJsYW5ndWFnZSI6IkVuZ2xpc2giLCJkYXRlX3N0YXJ0ZWQiOiIyMDE3LTA3LTE0IDExOjM3OjIwIEVEVCIsImxpbmtfaWQiOm51bGwsInVybF92YXJpYWJsZXMiOltdLCJpcF9hZGRyZXNzIjoiMTAwLjM4LjE1MS4xMTQiLCJyZWZlcmVyIjpudWxsLCJ1c2VyX2FnZW50IjoiU3VydmV5R2l6bW8gUkVTVCBBUEkiLCJyZXNwb25zZV90aW1lIjpudWxsLCJkYXRhX3F1YWxpdHkiOltdLCJsb25naXR1ZGUiOiItNzMuOTkwNjAwNTg1OTM4IiwibGF0aXR1ZGUiOiI0MC42OTQ0MDA3ODczNTQiLCJjb3VudHJ5IjoiVW5pdGVkIFN0YXRlcyIsImNpdHkiOiJCcm9va2x5biIsInJlZ2lvbiI6Ik5ZIiwicG9zdGFsIjoiMTEyMDEiLCJkbWEiOiI1MDEiLCJzdXJ2ZXlfZGF0YSI6eyI2Ijp7ImlkIjo2LCJ0eXBlIjoiTUVOVSIsInF1ZXN0aW9uIjoiVGVzdCBzdXJ2ZXkgcXVlc3Rpb241IGZpbGUiLCJzZWN0aW9uX2lkIjoxLCJhbnN3ZXIiOiJCb3RoIiwiYW5zd2VyX2lkIjoxMDAxMSwic2hvd24iOnRydWV9LCI1Ijp7ImlkIjo1LCJ0eXBlIjoicGFyZW50IiwicXVlc3Rpb24iOiJUZXN0IHN1cnZleSBxdWVzdGlvbjQgY2hlY2tib3giLCJzZWN0aW9uX2lkIjoxLCJvcHRpb25zIjp7IjEwMDA3Ijp7ImlkIjoxMDAwNywib3B0aW9uIjoiWWVzIiwiYW5zd2VyIjoiWWVzIn19LCJzaG93biI6dHJ1ZX0sIjQiOnsiaWQiOjQsInR5cGUiOiJFU1NBWSIsInF1ZXN0aW9uIjoiVGVzdCBzdXJ2ZXkgcXVlc3Rpb24zIGNvbW1lbnQiLCJzZWN0aW9uX2lkIjoxLCJhbnN3ZXIiOiJIZXJlIGlzIG15IHJlc3BvbnMzIiwic2hvd24iOnRydWV9LCIzIjp7ImlkIjozLCJ0eXBlIjoiTUVOVSIsInF1ZXN0aW9uIjoiVGVzdCBzdXJ2ZXkgcXVlc3Rpb24yIGRyb3Bkb3duIiwic2VjdGlvbl9pZCI6MSwiYW5zd2VyIjoib3B0aW9uMHZhbCIsImFuc3dlcl9pZCI6MTAwMDEsInNob3duIjp0cnVlfSwiMiI6eyJpZCI6MiwidHlwZSI6IlRFWFRCT1giLCJxdWVzdGlvbiI6IlRlc3Qgc3VydmV5IHF1ZXN0aW9uIHRleHQgZmllbGQiLCJzZWN0aW9uX2lkIjoxLCJhbnN3ZXIiOiJIZXJlIGlzIG15IHJlc3BvbnNlMSIsInNob3duIjp0cnVlfX19DQogICAgICAgICAgICB9";
            var data = Encoding.UTF8.GetString(Convert.FromBase64String(rawData));

            var surveyResponse = JsonConvert.DeserializeObject<SurveyResponse>(data);
            Assert.AreEqual("1", surveyResponse.id);

        }

        [TestMethod()]
        public void Create_Survey_Response_Test()
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
            var firstQuestionTitle = new LocalizableString("Test survey question text field");
            var secondQuestionTitle = new LocalizableString("Test survey question2 dropdown");
            var thirdQuestionTitle = new LocalizableString("Test survey question3 comment");
            var fourthQuestionTitle = new LocalizableString("Test survey question4 checkbox");
            var fifthQuestionTitle = new LocalizableString("Test survey question5 file");

            var q1 = apiClient.CreateQuestion(surveyId, 1, "text", firstQuestionTitle, "q1Short", null);
            var q2 = apiClient.CreateQuestion(surveyId, 1, "menu", secondQuestionTitle, "q2Short", null);
            var q3 = apiClient.CreateQuestion(surveyId, 1, "essay", thirdQuestionTitle, "q3Short", null);
            var q4 = apiClient.CreateQuestion(surveyId, 1, "checkbox", fourthQuestionTitle, "q4Short", null);
            var q5 = apiClient.CreateQuestion(surveyId, 1, "menu", fifthQuestionTitle, "q5Short", null);

            // get questions
            var questions = apiClient.GetQuestions(surveyId);
            Assert.IsNotNull(questions);
            Assert.IsTrue(questions.Any(i => i.id == q1.id));
            Assert.IsTrue(questions.Any(i => i.id == q2.id));
            Assert.IsTrue(questions.Any(i => i.id == q3.id));
            Assert.IsTrue(questions.Any(i => i.id == q4.id));
            Assert.IsTrue(questions.Any(i => i.id == q5.id));

            // add question options
            List<SurveyQuestionOption> surveyQuestionOptions = new List<SurveyQuestionOption>();
            for (var i = 0; i <= 5; i++)
            {
                var questionOp = new SurveyQuestionOption();
                var questionOptionTitle = new LocalizableString("option" + i);
                questionOp.title = questionOptionTitle;
                questionOp.value = $"option{i}val";

                questionOp.id = apiClient.CreateQuestionOption(surveyId, 1, q2.id, null, questionOp.title, questionOp.value);
                surveyQuestionOptions.Add(questionOp);
            }
            // get question options for question 2
            foreach (var op in surveyQuestionOptions)
            {
                var getQuestionOp = apiClient.GetQuestionOption(surveyId, q2.id, op.id);
                Assert.AreEqual(op.id, getQuestionOp.id);
            }

            //create quesiton options for checkbox
            var yesNoOption = new LocalizableString("Yes");
            var yesNoOption2 = new LocalizableString("No");
            var q4Option1 = apiClient.CreateQuestionOption(surveyId, 1, q4.id, null, yesNoOption, yesNoOption.English);
            var q4Option2 = apiClient.CreateQuestionOption(surveyId, 1, q4.id, null, yesNoOption2, yesNoOption2.English);

            //create quesiton options for question5
            var q5yesNoOption = new LocalizableString("Yes");
            var q5yesNoOption2 = new LocalizableString("No");
            var q5yesNoOption3 = new LocalizableString("Both");
            var q5Option1 = apiClient.CreateQuestionOption(surveyId, 1, q5.id, null, q5yesNoOption, q5yesNoOption.English);
            var q5Option2 = apiClient.CreateQuestionOption(surveyId, 1, q5.id, null, q5yesNoOption2, q5yesNoOption2.English);
            var q5Option3 = apiClient.CreateQuestionOption(surveyId, 1, q5.id, null, q5yesNoOption3, q5yesNoOption3.English);

            //create survey campaign
            var campaign = apiClient.CreateCampaign(surveyId, "testCampaign");
            Assert.AreEqual(campaign, apiClient.GetCampaign(surveyId, campaign).id);

            // create survey contact
            var fakeContact = new Contact();
            fakeContact.semailaddress = "U_test12345@tntp.org";
            fakeContact.sfirstname = "John";
            fakeContact.slastname = "Doe";
            fakeContact.sorganization = "Test sorganization";
            //var contactId = apiClient.CreateContact(surveyId, campaign, fakeContact);
           // Assert.AreEqual(contactId, apiClient.GetContact(surveyId, campaign, contactId).id);

            // create survey Response
            var r1 = new SurveyResponseQuestionData()
            {
                questionId = q1.id,
                questionShortName = q1.shortName,
                questionOptionIdentifier = null,
                value = "Here is my response1",
                isResponseAComment = false
            };
            var r2 = new SurveyResponseQuestionData()//comments dont count as questions
            {
                questionId = q1.id,
                questionShortName = q1.shortName,
                questionOptionIdentifier = null,
                value = "Here is my comment for response 1",
                isResponseAComment = true
            };
            var r3 = new SurveyResponseQuestionData()
            {
                questionId = q2.id,
                questionShortName = q2.shortName,
                questionOptionIdentifier = surveyQuestionOptions[0].id,
                value = surveyQuestionOptions[0].value,
                isResponseAComment = false
            };
            var r4 = new SurveyResponseQuestionData()
            {
                questionId = q3.id,
                questionShortName = q3.shortName,
                questionOptionIdentifier = null,
                value = "Here is my respons3",
                isResponseAComment = false
            };
            var r5 = new SurveyResponseQuestionData()
            {
                questionId = q4.id,
                questionShortName = q4.shortName,
                questionOptionIdentifier = q4Option1,
                value = "Yes",
                isResponseAComment = false
            };
            var r6 = new SurveyResponseQuestionData()
            {
                questionId = q5.id,
                questionShortName = q5.shortName,
                questionOptionIdentifier = q5Option3,
                value = "Both",
                isResponseAComment = false
            };
          
            List<SurveyResponseQuestionData> data = new List<SurveyResponseQuestionData>()
            {
                r1, r2, r3, r4, r5,r6
            };

           var getResponse = apiClient.CreateSurveyResponse(surveyId, "Saved", data);

            Assert.IsNotNull(getResponse);
            Assert.IsTrue(Convert.ToInt32(getResponse.id) > 0);

            // get survey Response
            var response = apiClient.GetResponse(surveyId, getResponse.id);
            Assert.AreEqual(response.id, getResponse.id);

            // get all survey Responses
            var allResponses = apiClient.GetResponses(surveyId);
            Assert.AreEqual(allResponses.Count, 1);
            Assert.IsTrue(allResponses.Any(i => i.id == response.id));
            Assert.AreEqual(allResponses[0].AllQuestions.Count, 5);
            //Assert.IsTrue(allResponses.Contains(response));
        }
        [TestMethod()]
        public void Update_Qcode_test()
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
            var firstQuestionTitle = new LocalizableString("Test survey question text field");
            var secondQuestionTitle = new LocalizableString("Test survey question2 dropdown");
            var thirdQuestionTitle = new LocalizableString("Test survey question3 comment");
            var fourthQuestionTitle = new LocalizableString("Test survey question4 checkbox");
            var fifthQuestionTitle = new LocalizableString("Test survey question5 file");

            var descrip5 = new QuestionProperties();
            descrip5.question_description = new Models.LocalizableString();
            descrip5.question_description.English = "Q5";

            var q1 = apiClient.CreateQuestion(surveyId, 1, "text", firstQuestionTitle, "q1Short", null);
            var q2 = apiClient.CreateQuestion(surveyId, 1, "menu", secondQuestionTitle, "q2Short", null);
            var q3 = apiClient.CreateQuestion(surveyId, 1, "essay", thirdQuestionTitle, "q3Short", null);
            var q4 = apiClient.CreateQuestion(surveyId, 1, "checkbox", fourthQuestionTitle, "q4Short", null);
            var q5 = apiClient.CreateQuestion(surveyId, 1, "menu", fifthQuestionTitle, "q5Short", descrip5);

            // get questions
            var questions = apiClient.GetQuestions(surveyId);
            Assert.IsNotNull(questions);
            Assert.IsTrue(questions.Any(i => i.id == q1.id));
            Assert.IsTrue(questions.Any(i => i.id == q2.id));
            Assert.IsTrue(questions.Any(i => i.id == q3.id));
            Assert.IsTrue(questions.Any(i => i.id == q4.id));
            Assert.IsTrue(questions.Any(i => i.id == q5.id));

            var updateQCode = apiClient.UpdateQcodeOfSurveyQuestion(surveyId, q5.id, "UpdatedQ5");
            Assert.IsTrue(updateQCode);

            var updatedQuestions = apiClient.GetQuestions(surveyId);
            var updatedqcodeq5 = updatedQuestions.Where(i => i.id == q5.id)
                .Select(i => i.properties.question_description.English).First();
            Assert.AreEqual(updatedqcodeq5, "<span style=\\\"font-size:0px;\\\">UpdatedQ5</span>");
        }
    }
}
