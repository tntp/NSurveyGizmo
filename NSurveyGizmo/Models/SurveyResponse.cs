using Newtonsoft.Json;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Newtonsoft.Json.Serialization;
using NSurveyGizmo.Models;

namespace NSurveyGizmo.Models
{
    // Here is the magic: When you see this type, use this class to read it.
    // If you want, you can also define the JsonConverter by adding it to
    // a JsonSerializer, and parsing with that.
    [JsonObject]
    [JsonConverter(typeof(DataItemConverter))]
    public class SurveyResponse
    {
        public bool result_ok { get; set; }
        public object data { get; set; }
        public string id { get; set; }
        public string contact_id { get; set; }
        public string status { get; set; }
        public string is_test_data { get; set; }
        public DateTime date_submitted { get; set; }
        [JsonProperty("comments")]
        public string sResponseComment { get; set; }
        [JsonProperty("survey_data")]
        public List<SurveyQuestion> SurveyQuestions { get; set; } = new List<SurveyQuestion>();
        [JsonProperty("url_variables")]
        public List<SurveyUrl> SurveyUrls { get; set; } = new List<SurveyUrl>();
        public List<SurveyGeoData> SurveyGeoDatas { get; set; } = new List<SurveyGeoData>();
        public List<SurveyVariable> SurveyVariables { get; set; } = new List<SurveyVariable>();
        public List<SurveyVariableShown> SurveyVariableShowns { get; set; } = new List<SurveyVariableShown>();
        public List<SurveyQuestionHidden> SurveyQuestionHiddens { get; set; } = new List<SurveyQuestionHidden>();
        public List<SurveyQuestionOption> SurveyQuestionOptions { get; set; } = new List<SurveyQuestionOption>();
        public List<SurveyQuestionMulti> SurveyQuestionMulties { get; set; } = new List<SurveyQuestionMulti>();
        public Dictionary<int, string> AllQuestions { get; set; } = new Dictionary<int, string>();

        public void AddQuestion(int key, string value)
        {
            if (!AllQuestions.ContainsKey(key))
            {
                AllQuestions.Add(key, value);
                return;
            }

            AllQuestions[key] += "," + value;
        }

        public bool Equals(SurveyResponse sr)
        {
            return id                  == sr.id
                   && contact_id       == sr.contact_id
                   && status           == sr.status
                   && is_test_data     == sr.is_test_data
                   && date_submitted == sr.date_submitted
                   && sResponseComment == sr.sResponseComment
                   && SurveyQuestions.SequenceEqual(sr.SurveyQuestions)
                   && SurveyUrls.SequenceEqual(sr.SurveyUrls)
                   && SurveyGeoDatas.SequenceEqual(sr.SurveyGeoDatas)
                   && SurveyVariables.SequenceEqual(sr.SurveyVariables)
                   && SurveyVariableShowns.SequenceEqual(sr.SurveyVariableShowns)
                   && SurveyQuestionHiddens.SequenceEqual(sr.SurveyQuestionHiddens)
                   && SurveyQuestionOptions.SequenceEqual(sr.SurveyQuestionOptions)
                   && SurveyQuestionMulties.SequenceEqual(sr.SurveyQuestionMulties)
                   && AllQuestions.OrderBy(kvp => kvp.Key)
                                  .SequenceEqual(sr.AllQuestions.OrderBy(kvp => kvp.Key));
        }
    }

    public class JsonDictToArrayAttribute : Attribute   {    }

    public class SurveyGeoData
    {
        [Key]
        public int SurveyGeoDataID { get; set; }
        public string Name { get; set; }
        public string Value { get; set; }

        public bool Equals(SurveyGeoData sgd)
        {
            return SurveyGeoDataID == sgd.SurveyGeoDataID
                && Name            == sgd.Name
                && Value           == sgd.Value;
        }
    }

    public class SurveyQuestionHidden
    {
        [Key]
        public int QuestionID { get; set; }
        public string QuestionResponse { get; set; }

        public bool Equals(SurveyQuestionHidden sqh)
        {
            return QuestionID       == sqh.QuestionID
                && QuestionResponse == sqh.QuestionResponse;
        }
    }

    public class SurveyUrl
    {
        [Key]
        public int SurveyUrlID { get; set; }
        public string Name { get; set; }
        public string Value { get; set; }

        public bool Equals(SurveyUrl su)
        {
            return SurveyUrlID == su.SurveyUrlID
                && Name        == su.Name
                && Value       == su.Value;
        }
    }

    public class SurveyVariable
    {
        [Key]
        public int SurveyVariableID { get; set; }
        public string Value { get; set; }

        public bool Equals(SurveyVariable sv)
        {
            return SurveyVariableID == sv.SurveyVariableID && Value == sv.Value;
        }
    }

    public class SurveyVariableShown
    {
        [Key]
        public int SurveyVariableShownID { get; set; }
        public string Name { get; set; }
        public string Value { get; set; }

        public bool Equals(SurveyVariableShown svs)
        {
            return SurveyVariableShownID == svs.SurveyVariableShownID
                && Name  == svs.Name
                && Value == svs.Value;
        }
    }

    public class SurveyResponseQuestionData
    {
        public int? questionId { get; set; }
        public string questionShortName { get; set; }
        public int? questionOptionIdentifier { get; set; }
        public string value { get; set; }
        public bool isResponseAComment { get; set; }
        public string questionOptionTitle { get; set; }

        public bool Equals(SurveyResponseQuestionData srqd)
        {
            return questionId               == srqd.questionId
                && questionShortName        == srqd.questionShortName
                && questionOptionIdentifier == srqd.questionOptionIdentifier
                && value                    == srqd.value
                && isResponseAComment == srqd.isResponseAComment
                && questionOptionTitle      == srqd.questionOptionTitle;
        }
    }
}
