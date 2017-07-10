using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace NSurveyGizmo.Models
{
    // Here is the magic: When you see this type, use this class to read it.
    // If you want, you can also define the JsonConverter by adding it to
    // a JsonSerializer, and parsing with that.
    [JsonObject, JsonConverter(typeof(DataItemConverter))]
    public class SurveyResponse
    {
        public string id { get; set; }
        public string contact_id { get; set; }
        public string status { get; set; }
        public string is_test_data { get; set; }
        public DateTime datesubmitted { get; set; }
        public string sResponseComment { get; set; }
        public List<SurveyQuestion> SurveyQuestions { get; set; }
        public List<SurveyUrl> SurveyUrls { get; set; }
        public List<SurveyGeoData> SurveyGeoDatas { get; set; }
        public List<SurveyVariable> SurveyVariables { get; set; }
        public List<SurveyVariableShown> SurveyVariableShowns { get; set; }
        public List<SurveyQuestionHidden> SurveyQuestionHiddens { get; set; }
        public List<SurveyQuestionOption> SurveyQuestionOptions { get; set; }
        public List<SurveyQuestionMulti> SurveyQuestionMulties { get; set; }
        public Dictionary<int, string> AllQuestions { get; set; }

        public void AddQuestion(int key, string value)
        {
            if (!AllQuestions.ContainsKey(key))
            {
                AllQuestions.Add(key, value);
                return;
            }

            AllQuestions[key] += "," + value;
        }
    }

    public class SurveyGeoData
    {
        [Key]
        public int SurveyGeoDataID { get; set; }

        public string Name { get; set; }
        public string Value { get; set; }
    }

    public class SurveyQuestionHidden
    {
        [Key]
        public int QuestionID { get; set; }

        public string QuestionResponse { get; set; }
    }

    public class SurveyUrl
    {
        [Key]
        public int SurveyUrlID { get; set; }
        public string Name { get; set; }
        public string Value { get; set; }
    }

    public class SurveyVariable
    {
        [Key]
        public int SurveyVariableID { get; set; }

        public string Value { get; set; }
    }

    public class SurveyVariableShown
    {
        [Key]
        public int SurveyVariableShownID { get; set; }

        public string Name { get; set; }
        public string Value { get; set; }
    }
}
