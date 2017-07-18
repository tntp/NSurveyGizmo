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
            var contactId2 = apiClient.CreateContact(surveyId, campaign, fakeContact2);

            var fakeContact3 = new Contact();
            fakeContact3.semailaddress = "U_test8910@tntp.org";
            fakeContact3.sfirstname = "Frank";
            fakeContact3.slastname = "Sinatra";
            fakeContact3.sorganization = "Test sorganization";
            var contactId3 = apiClient.CreateContact(surveyId, campaign, fakeContact3);

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
            var contactId = apiClient.CreateContact(surveyId, campaign, fakeContact);
            Assert.AreEqual(contactId, apiClient.GetContact(surveyId, campaign, contactId).id);

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
            Assert.IsTrue(allResponses[0].AllQuestions.ContainsKey(2));
            Assert.IsTrue(allResponses[0].AllQuestions.ContainsValue("Here is my response1"));
            Assert.IsTrue(allResponses[0].AllQuestions.ContainsKey(3));
            Assert.IsTrue(allResponses[0].AllQuestions.ContainsValue("option0val"));
            Assert.IsTrue(allResponses[0].AllQuestions.ContainsKey(4));
            Assert.IsTrue(allResponses[0].AllQuestions.ContainsValue("Here is my respons3"));
            Assert.IsTrue(allResponses[0].AllQuestions.ContainsKey(5));
            Assert.IsTrue(allResponses[0].AllQuestions.ContainsValue("Yes"));
            Assert.IsTrue(allResponses[0].AllQuestions.ContainsKey(6));
            Assert.IsTrue(allResponses[0].AllQuestions.ContainsValue("Both"));


            //Assert.AreEqual(allResponses[0].SurveyGeoDatas.Count, 28); no longer relavent
            Assert.AreEqual(allResponses[0].SurveyGeoDatas[0].Name, "STANDARD_IP");
            Assert.IsNotNull(allResponses[0].SurveyGeoDatas[0].Value);
            Assert.AreEqual(allResponses[0].SurveyGeoDatas[1].Name, "STANDARD_REFERER");
            Assert.AreEqual(allResponses[0].SurveyGeoDatas[2].Name, "STANDARD_USERAGENT");
            Assert.AreEqual(allResponses[0].SurveyGeoDatas[3].Name, "STANDARD_RESPONSETIME");
            Assert.AreEqual(allResponses[0].SurveyGeoDatas[4].Name, "STANDARD_LONG");
            Assert.IsNotNull(allResponses[0].SurveyGeoDatas[4].Value);
            Assert.AreEqual(allResponses[0].SurveyGeoDatas[5].Name, "STANDARD_LAT");
            Assert.IsNotNull(allResponses[0].SurveyGeoDatas[5].Value);
            Assert.AreEqual(allResponses[0].SurveyGeoDatas[6].Name, "STANDARD_GEOCOUNTRY");
            Assert.IsNotNull(allResponses[0].SurveyGeoDatas[6].Value);
            Assert.AreEqual(allResponses[0].SurveyGeoDatas[7].Name, "STANDARD_GEOCITY");
            Assert.IsNotNull(allResponses[0].SurveyGeoDatas[7].Value);
            Assert.AreEqual(allResponses[0].SurveyGeoDatas[8].Name, "STANDARD_GEOREGION");
            Assert.IsNotNull(allResponses[0].SurveyGeoDatas[8].Value);
            Assert.AreEqual(allResponses[0].SurveyGeoDatas[9].Name, "STANDARD_GEOPOSTAL");
            Assert.IsNotNull(allResponses[0].SurveyGeoDatas[9].Value);
            Assert.AreEqual(allResponses[0].SurveyGeoDatas[10].Name, "STANDARD_GEODMA");
            Assert.AreEqual(allResponses[0].SurveyGeoDatas[10].Value, "501");

            //These don't exist anymore
            //Assert.AreEqual(allResponses[0].SurveyGeoDatas[8].Name, "STANDARD_COMMENTS"); 
            //Assert.AreEqual(allResponses[0].SurveyGeoDatas[9].Name, "STANDARD_DEVICE");
            //Assert.AreEqual(allResponses[0].SurveyGeoDatas[9].Value, "Desktop");
            //Assert.AreEqual(allResponses[0].SurveyGeoDatas[10].Name, "STANDARD_DATAQUALITYCOUNTER");
            //Assert.AreEqual(allResponses[0].SurveyGeoDatas[11].Name, "STANDARD_CHECKBOXONE_COUNT");
            //Assert.AreEqual(allResponses[0].SurveyGeoDatas[12].Name, "STANDARD_CHECKBOXONE");
            //Assert.AreEqual(allResponses[0].SurveyGeoDatas[13].Name, "STANDARD_STRAIGHTLINING_COUNT");
            //Assert.AreEqual(allResponses[0].SurveyGeoDatas[14].Name, "STANDARD_STRAIGHTLINING");
            //Assert.AreEqual(allResponses[0].SurveyGeoDatas[15].Name, "STANDARD_OPENTEXTGIBBERISH_COUNT");
            //Assert.AreEqual(allResponses[0].SurveyGeoDatas[16].Name, "STANDARD_OPENTEXTGIBBERISH");
            //Assert.AreEqual(allResponses[0].SurveyGeoDatas[17].Name, "STANDARD_OPENTEXTBADWORDS_COUNT");
            //Assert.AreEqual(allResponses[0].SurveyGeoDatas[18].Name, "STANDARD_OPENTEXTBADWORDS");
            //Assert.AreEqual(allResponses[0].SurveyGeoDatas[19].Name, "STANDARD_OPENTEXTONEWORDREQUIREDESSAY_COUNT");
            //Assert.AreEqual(allResponses[0].SurveyGeoDatas[20].Name, "STANDARD_OPENTEXTONEWORDREQUIREDESSAY");
            //Assert.AreEqual(allResponses[0].SurveyGeoDatas[21].Name, "STANDARD_CHECKBOXALL_COUNT");
            //Assert.AreEqual(allResponses[0].SurveyGeoDatas[22].Name, "STANDARD_CHECKBOXALL");
            //Assert.AreEqual(allResponses[0].SurveyGeoDatas[23].Name, "STANDARD_AVGQUESTSECONDS");
            //Assert.AreEqual(allResponses[0].SurveyGeoDatas[24].Name, "STANDARD_FINGERPRINT");

            Assert.AreEqual(allResponses[0].SurveyQuestionHiddens.Count, 0);

            Assert.AreEqual(allResponses[0].SurveyQuestionMulties.Count, 1);
            Assert.AreEqual(allResponses[0].SurveyQuestionMulties[0].OptionID, 10007);
            Assert.AreEqual(allResponses[0].SurveyQuestionMulties[0].QuestionID, 5);
            Assert.AreEqual(allResponses[0].SurveyQuestionMulties[0].QuestionResponse, "Yes");

            Assert.AreEqual(allResponses[0].SurveyQuestionOptions.Count, 0);

            Assert.AreEqual(allResponses[0].SurveyQuestions.Count, 4);
            Assert.AreEqual(allResponses[0].SurveyQuestions[3].QuestionResponse, "Here is my response1");
            Assert.AreEqual(allResponses[0].SurveyQuestions[3].id, 2);
            Assert.AreEqual(allResponses[0].SurveyQuestions[2].QuestionResponse, "option0val");
            Assert.AreEqual(allResponses[0].SurveyQuestions[2].id, 3);
            Assert.AreEqual(allResponses[0].SurveyQuestions[1].QuestionResponse, "Here is my respons3");
            Assert.AreEqual(allResponses[0].SurveyQuestions[1].id, 4);
            Assert.AreEqual(allResponses[0].SurveyQuestions[0].QuestionResponse, "Both");
            Assert.AreEqual(allResponses[0].SurveyQuestions[0].id, 6);

            Assert.AreEqual(allResponses[0].SurveyUrls.Count, 0);

            Assert.AreEqual(allResponses[0].SurveyVariableShowns.Count, 5);
            //Assert.AreEqual(allResponses[0].SurveyVariableShowns.Count, 6);
            
            //Assert.AreEqual(allResponses[0].SurveyVariableShowns[0].Name, "PORTAL_RELATIONSHIP");  
            //Assert.AreEqual(allResponses[0].SurveyVariableShowns[0].SurveyVariableShownID, 0);
            //Assert.AreEqual(allResponses[0].SurveyVariableShowns[0].Value, "");
            Assert.AreEqual(allResponses[0].SurveyVariableShowns[4].Name, "2-shown");
            Assert.AreEqual(allResponses[0].SurveyVariableShowns[4].SurveyVariableShownID, 0);
            Assert.AreEqual(allResponses[0].SurveyVariableShowns[4].Value, "1");
            Assert.AreEqual(allResponses[0].SurveyVariableShowns[3].Name, "3-shown");
            Assert.AreEqual(allResponses[0].SurveyVariableShowns[3].SurveyVariableShownID, 0);
            Assert.AreEqual(allResponses[0].SurveyVariableShowns[3].Value, "1");
            Assert.AreEqual(allResponses[0].SurveyVariableShowns[2].Name, "4-shown");
            Assert.AreEqual(allResponses[0].SurveyVariableShowns[2].SurveyVariableShownID, 0);
            Assert.AreEqual(allResponses[0].SurveyVariableShowns[2].Value, "1");
            Assert.AreEqual(allResponses[0].SurveyVariableShowns[1].Name, "5-shown");
            Assert.AreEqual(allResponses[0].SurveyVariableShowns[1].SurveyVariableShownID, 0);
            Assert.AreEqual(allResponses[0].SurveyVariableShowns[1].Value, "1");
            Assert.AreEqual(allResponses[0].SurveyVariableShowns[0].Name, "6-shown");
            Assert.AreEqual(allResponses[0].SurveyVariableShowns[0].SurveyVariableShownID, 0);
            Assert.AreEqual(allResponses[0].SurveyVariableShowns[0].Value, "1");

            Assert.AreEqual(allResponses[0].SurveyVariables.Count, 2);
            Assert.AreEqual(allResponses[0].SurveyVariables[1].SurveyVariableID, 3);
            Assert.AreEqual(allResponses[0].SurveyVariables[1].Value, "10001");
            Assert.AreEqual(allResponses[0].SurveyVariables[0].SurveyVariableID, 6);
            Assert.AreEqual(allResponses[0].SurveyVariables[0].Value, "10011");


            Assert.AreEqual(allResponses[0].contact_id, "");

            Assert.AreEqual(allResponses[0].sResponseComment, null);

            Assert.AreEqual(allResponses[0].status, "Complete");

            Assert.IsNotNull(allResponses[0].datesubmitted);

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

        [TestMethod()]
        public void Create_and_Update_ContactList_test()
        {
            var contact = new Contact()
            {
                semailaddress = "myemail@tntp.org",
                sfirstname = "testname",
                slastname = "testname",
                sorganization = "testorg"
            };

            var contactList = apiClient.CreateContactList("coolNewList");
            var updatedList = apiClient.UpdateContactList(contactList, contact.semailaddress, contact.sfirstname, contact.slastname,
                contact.sorganization, null);
            var getUpdatedContactList = apiClient.GetContactList(contactList);
            var getAllContactsForList = apiClient.GetAllContactsForContactList(getUpdatedContactList);
            Assert.AreEqual(getAllContactsForList.Count, 1);
            Assert.IsNotNull(getUpdatedContactList);
            Assert.AreEqual(getUpdatedContactList, contactList);
            Assert.IsTrue(updatedList);

            // create survey
            var title = "Test Survey " + testStartedAt;
            var surveyId = apiClient.CreateSurvey(title);
            Assert.IsTrue(surveyId > 0);

            //create survey campaign
            var campaign = apiClient.CreateCampaign(surveyId, "testCampaign");
            Assert.AreEqual(campaign, apiClient.GetCampaign(surveyId, campaign).id);

            var createNewContact = apiClient.CreateContact(surveyId, campaign, contact);

            var getUpdatedList = apiClient.GetCampaignContactList(surveyId, campaign);
            Assert.IsTrue(getUpdatedList.Count > 0);
        }
    }
}
