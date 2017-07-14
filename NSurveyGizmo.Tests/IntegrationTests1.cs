using System;
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
            Assert.IsTrue(question.shortName == shortName);

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

        // TODO: Finish implementing
        //[TestMethod]
        //public void SurveyResponse_Tests()
        //{
        //    var surveyId = apiClient.CreateSurvey(_surveyTitle);

        //    // sd.questionId, sd.questionShortName, sd.questionOptionIdentifier, sd.value, sd.isResonseAComment, sd.questionOptionTitle
        //    var questionData = new List<SurveyResponseQuestionData>
        //    {
        //        new SurveyResponseQuestionData(1, "q1", 1, "True", false, "Question 1")
        //    };
        //    var surveyResponse = apiClient.CreateSurveyResponse(surveyId, "statusnotused", questionData);

        //    var retrievedResponses = apiClient.GetResponses(surveyId);
        //}
    }
}
