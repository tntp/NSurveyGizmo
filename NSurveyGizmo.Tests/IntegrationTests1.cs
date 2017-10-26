using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSurveyGizmo.Models;

namespace NSurveyGizmo.Tests
{
    public partial class IntegrationTests
    {
        private string _surveyTitle;
        private string _campaignName;
        private DateTime testStartedAt;
        private ApiClient apiClient;

        [TestInitialize]
        public void Initialize()
        {
            var appSettings = ConfigurationManager.AppSettings;
            apiClient = new ApiClient
            {
                ApiToken = appSettings["ApiToken"],
                ApiTokenSecret = appSettings["ApiTokenSecret"]
            };

            testStartedAt = DateTime.Now;
            _surveyTitle  = $"TestSurvey_{DateTime.Now:yyyyMMdd_HHmmss}";
            _campaignName = $"TestCampaign_{DateTime.Now:yyyyMMdd_HHmmss}";
        }

        [TestMethod]
        public void Survey_Tests()
        {
            var surveyId = apiClient.CreateSurvey(_surveyTitle);
            Assert.IsTrue(surveyId > 0);

            var retrievedSurvey = apiClient.GetSurvey(surveyId);
            Assert.IsTrue(retrievedSurvey.title == _surveyTitle);

            var allSurveys = apiClient.GetAllSurveys();
            Assert.IsTrue(allSurveys.Count(s => s.title == _surveyTitle) == 1);

            var success = apiClient.DeleteSurvey(surveyId);
            Assert.IsTrue(success);

            retrievedSurvey = apiClient.GetSurvey(surveyId);
            Assert.IsTrue(retrievedSurvey.status == "Deleted");
        }

        [TestMethod]
        public void Campaign_Tests()
        {
            var surveyId = apiClient.CreateSurvey(_surveyTitle);

            var campaignId = apiClient.CreateCampaign(surveyId, _campaignName);
            Assert.IsTrue(campaignId > 0);

            var retrievedCampaign = apiClient.GetCampaign(surveyId, campaignId);
            Assert.IsTrue(retrievedCampaign.name == _campaignName);

            var allSurveyCampaigns = apiClient.GetCampaigns(surveyId);
            Assert.IsTrue(allSurveyCampaigns.Count(s => s.name == _campaignName) == 1);

            var updatedCampaginName = "Updated" + _campaignName;
            retrievedCampaign.name = updatedCampaginName;
            var success = apiClient.UpdateCampaign(surveyId, retrievedCampaign);
            Assert.IsTrue(success);

            retrievedCampaign = apiClient.GetCampaign(surveyId, campaignId);
            Assert.IsTrue(retrievedCampaign.name == updatedCampaginName);

            success = apiClient.DeleteCampaign(surveyId, campaignId);
            Assert.IsTrue(success);

            retrievedCampaign = apiClient.GetCampaign(surveyId, campaignId);
            Assert.IsNull(retrievedCampaign);
        }

        [TestMethod]
        public void Question_Tests()
        {
            var surveyId = apiClient.CreateSurvey(_surveyTitle);

            var type = "checkbox";
            var title = new LocalizableString("Checkbox Question Title");
            var shortName = "chkBxQ";
            var questionProps = new QuestionProperties
            {
                question_description = new LocalizableString("Question Description"),
                required = true
            };

            var question = apiClient.CreateQuestion(surveyId, 1, type, title, shortName, questionProps);
            Assert.IsTrue(question._subtype == type);
            Assert.IsTrue(question.title.Equals(title));
            Assert.IsTrue(question.shortname == shortName);

            var questions = apiClient.GetQuestions(surveyId);
            Assert.IsTrue(questions.Count(sq => sq.Equals(question)) == 1);

            var newQCode = "newQCode";
            var success = apiClient.UpdateQcodeOfSurveyQuestion(surveyId, question.id, newQCode);
            question = apiClient.GetQuestions(surveyId).First(sq => sq.id == question.id);
            Assert.IsTrue(success);

            // UpdateQcodeOfSurveyQuestion wraps the new question code in a span tag -- <span style="font-size:0px;">{newQCode}</span>
            Assert.IsTrue(question.properties.question_description.English.Contains(newQCode));
        }

        [TestMethod]
        public void Contact_Tests()
        {
            var surveyId = apiClient.CreateSurvey(_surveyTitle);
            var campaignId = apiClient.CreateCampaign(surveyId, _campaignName);

            // Create-Update with parameter list //

            // Create
            var email = "U_john.doe@tntp.org";
            var firstName = "John";
            var lastName = "Doe";
            var organization = "Test Organization A";
            // TODO: Implement custom fields // var customFields = new string[] {};
            var contactId1 = apiClient.CreateContact(surveyId, campaignId, email, firstName, lastName, organization, null);
            Assert.IsTrue(contactId1 > 0);

            // Verify Crate
            var retrievedContact1 = apiClient.GetContact(surveyId, campaignId, contactId1);
            Assert.IsTrue(retrievedContact1.semailaddress == email);
            Assert.IsTrue(retrievedContact1.sfirstname == firstName);
            Assert.IsTrue(retrievedContact1.slastname == lastName);
            Assert.IsTrue(retrievedContact1.sorganization == organization);

            // Update
            email = "U_jonathan.dow@tntp.org";
            firstName = "Jonathan";
            lastName = "Dow";
            organization = "Test Organization Alpha";
            var success = apiClient.UpdateContact(surveyId, campaignId, contactId1, email, firstName, lastName, organization);
            Assert.IsTrue(success);

            // Verify Update
            retrievedContact1 = apiClient.GetContact(surveyId, campaignId, contactId1);
            Assert.IsTrue(retrievedContact1.semailaddress == email);
            Assert.IsTrue(retrievedContact1.sfirstname == firstName);
            Assert.IsTrue(retrievedContact1.slastname == lastName);
            Assert.IsTrue(retrievedContact1.sorganization == organization);

            // Create-Update with Contact object //

            // Create
            var contact = new Contact
            {
                semailaddress = "U_jane.doe@tntp.org",
                sfirstname = "Jane",
                slastname = "Doe",
                sorganization = "Test Organization B"
            };
            var contactId2 = apiClient.CreateContact(surveyId, campaignId, contact);
            contact.id = contactId2;
            Assert.IsTrue(contactId2 > 0);

            // Verify Create
            var retrievedContact2 = apiClient.GetContact(surveyId, campaignId, contactId2);
            Assert.IsTrue(retrievedContact2.semailaddress == contact.semailaddress);
            Assert.IsTrue(retrievedContact2.sfirstname == contact.sfirstname);
            Assert.IsTrue(retrievedContact2.slastname == contact.slastname);
            Assert.IsTrue(retrievedContact2.sorganization == contact.sorganization);

            // Update
            contact.semailaddress = "U_janice.dostoevsky@tntp.org";
            contact.sfirstname = "Janice";
            contact.slastname = "Dostoevsky";
            contact.sorganization = "Test Organization Beta";
            success = apiClient.UpdateContact(surveyId, campaignId, contactId2, contact);
            Assert.IsTrue(success);

            // Verify Update
            retrievedContact2 = apiClient.GetContact(surveyId, campaignId, contactId2);
            Assert.IsTrue(retrievedContact2.semailaddress == contact.semailaddress);
            Assert.IsTrue(retrievedContact2.sfirstname == contact.sfirstname);
            Assert.IsTrue(retrievedContact2.slastname == contact.slastname);
            Assert.IsTrue(retrievedContact2.sorganization == contact.sorganization);

            var contactList = apiClient.GetCampaignContactList(surveyId, campaignId);
            Assert.IsTrue(contactList.Any(c => new[] { contactId1, contactId2 }.Contains(c.id)));

            // Delete
            success = apiClient.DeleteContact(surveyId, campaignId, contactId1);
            Assert.IsTrue(success);

            success = apiClient.DeleteContact(surveyId, campaignId, contactId2);
            Assert.IsTrue(success);

            contactList = apiClient.GetCampaignContactList(surveyId, campaignId);
            Assert.IsTrue(contactList == null || !contactList.Any() || !contactList.Any(c => new [] {contactId1, contactId2}.Contains(c.id)));
        }

        [TestMethod]
        public void EmailMessage_Tests()
        {
            var surveyId = apiClient.CreateSurvey(_surveyTitle);
            var campaignId = apiClient.CreateCampaign(surveyId, _campaignName);

            var messages = apiClient.GetEmailMessageList(surveyId, campaignId);
            Assert.IsTrue(messages.Count == 1);

            var message = messages.First();
            message.from.name = "Guy Incognito";
            message.from.email = "modifiedemail@tntp.org";
            
            var success = apiClient.UpdateEmailMessage(surveyId, campaignId, message);
            Assert.IsTrue(success);

            var retrievedMessage = apiClient.GetEmailMessageList(surveyId, campaignId).First();
            Assert.IsTrue(retrievedMessage.from.name == message.from.name);
            Assert.IsTrue(retrievedMessage.from.email == message.from.email);
        }
        
        [TestMethod]
        public void Test_Sub_Questions()
        {
            //Create survey
            var title = "Test Survey " + testStartedAt;
            var surveyId = apiClient.CreateSurvey(title);
            Assert.IsTrue(surveyId > 0);

            //Create question
            var questionTitle = new LocalizableString("Enter your intials");
            var q0 = apiClient.CreateQuestion(surveyId, 1, "text", questionTitle, "Initials", null);

            //Create parent question
            var firstQuestionTitle = new LocalizableString("So far this year, how many times have you been observed in your classroom? Please divide the total number of classroom visits into “long” and “short” observations below:");
            var q1 = apiClient.CreateQuestion(surveyId, 1, "GROUP", firstQuestionTitle, "Observations", null);

            //Create first sub question
            var childQuestion1 = new LocalizableString("Short observations (15 minutes or less)");
            var childQuestion1Option1 = new LocalizableString("4");
            var childQuestion1Option2 = new LocalizableString("5");
            var subQ1 = apiClient.CreateQuestionRow(surveyId, 1, q1.id, "menu", childQuestion1, "shortobservations", null);
            apiClient.CreateQuestionOption(surveyId, 1, subQ1.id, null, childQuestion1Option1, "4");
            apiClient.CreateQuestionOption(surveyId, 1, subQ1.id, null, childQuestion1Option2, "5");

            //Create second sub question
            var childQuestion2 = new LocalizableString("Longer than 15 minutes, often a full class period");
            var childQuestion2Option1 = new LocalizableString("10");
            var childQuestion2Option2 = new LocalizableString("20");
            var subQ2 = apiClient.CreateQuestionRow(surveyId, 1, q1.id, "menu", childQuestion2, "longobservations", null);
            apiClient.CreateQuestionOption(surveyId, 1, subQ2.id, null, childQuestion2Option1, "10");
            apiClient.CreateQuestionOption(surveyId, 1, subQ2.id, null, childQuestion2Option2, "20");
            
            //Create spss variable names on parent
            var qCodes = new Dictionary<int, string>
            {
                {subQ1.id, "Obs_Total_Short"},
                {subQ2.id, "Obs_Total_Long"},
            };
            apiClient.UpdateQcodeOfSurveyQuestion(surveyId, qCodes, q1.id);

            //Get Question codes and assert - This will test the deserialize 
            var questions = apiClient.GetQuestions(surveyId, true);
            var parentQuestion = questions.SingleOrDefault(q => q.id == q1.id);
            Assert.IsTrue(parentQuestion!=null);
            Assert.AreEqual(parentQuestion.sub_questions.Length,2);

            var subQuestion1 = parentQuestion.sub_questions.SingleOrDefault(s => s.id == subQ1.id);
            Assert.IsTrue(subQuestion1!=null);

            var subQuestion2 = parentQuestion.sub_questions.SingleOrDefault(s => s.id == subQ2.id);
            Assert.IsTrue(subQuestion2 != null);

            //Create survey campaign
            var campaign = apiClient.CreateCampaign(surveyId, "testCampaign");

            //Create survey contact
            var fakeContact = new Contact
            {
                semailaddress = "U_test12345@tntp.org",
                sfirstname = "John",
                slastname = "Doe",
                sorganization = "Test sorganization"
            };
            var contactId = apiClient.CreateContact(surveyId, campaign, fakeContact);

            //Create survey Response
            var r1 = new SurveyResponseQuestionData
            {
                questionId = subQ1.id,
                questionShortName = subQ1.shortname,
                value = "5"
            };
            var r2 = new SurveyResponseQuestionData
            {
                questionId = subQ2.id,
                questionShortName = subQ2.shortname,
                value = "20"
            };
            var r3 = new SurveyResponseQuestionData
            {
                questionId = q0.id,
                questionShortName = q0.shortname,
                value = "JD"
            };
            var data = new List<SurveyResponseQuestionData>()
            {
                r1, r2,r3
            };
            var getResponse = apiClient.CreateSurveyResponse(surveyId, "Saved", data);

            //Get survey Responses - This will test the sub questions deserialization code
            var allResponses = apiClient.GetResponses(surveyId);
            var response = allResponses.FirstOrDefault();
            Assert.AreEqual(allResponses.Count,1);
            Assert.IsTrue(response!=null);
            Assert.AreEqual(response.AllQuestions.Count, 3);
            Assert.AreEqual(response.SurveyQuestions.Count, 2);

            //Assert the response
            var resp1 = response.AllQuestions.SingleOrDefault(s => s.Key == q0.id);
            var resp2 = response.AllQuestions.SingleOrDefault(s => s.Key == subQ1.id);
            var resp3 = response.AllQuestions.SingleOrDefault(s => s.Key == subQ2.id);
            Assert.AreEqual(resp1.Value,"JD");
            Assert.AreEqual(resp2.Value, "5");
            Assert.AreEqual(resp3.Value, "20");

            //Get the group question
            var groupQuestion = response.SurveyQuestions.FirstOrDefault(s => s.id == q1.id);
            Assert.AreEqual(groupQuestion.sub_questions.Length,2);
            var s1 = groupQuestion.sub_questions.SingleOrDefault(s => s.id == subQ1.id);
            var s2 = groupQuestion.sub_questions.SingleOrDefault(s => s.id == subQ2.id);
            Assert.AreEqual(s1.QuestionResponse, "5");
            Assert.AreEqual(s2.QuestionResponse, "20");
        }
    }
}