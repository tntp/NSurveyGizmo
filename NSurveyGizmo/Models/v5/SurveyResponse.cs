using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NSurveyGizmo.Models.v5;

namespace NSurveyGizmo.v5
{
    [JsonObject, JsonConverter(typeof(DataItemConverter))]
    public class SurveyResponse
    {
        public String id { get; set; }
        public string contact_id { get; set; }
        public string status { get; set; }
        public string is_test_data { get; set; }
        public DateTime date_submitted { get; set; }
        public string comments { get; set; }
        public List<SurveyQuestion> survey_data { get; set; }
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

        public bool Equals(SurveyResponse sr)
        {
            return id == sr.id
                   && contact_id == sr.contact_id
                   && status == sr.status
                   && is_test_data == sr.is_test_data
                   && date_submitted == sr.date_submitted
                   && comments == sr.comments
                   && survey_data.SequenceEqual(sr.survey_data)
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

    public class SurveyGeoData
    {
        [Key]
        public int SurveyGeoDataID { get; set; }
        public string Name { get; set; }
        public string Value { get; set; }

        public bool Equals(SurveyGeoData sgd)
        {
            return SurveyGeoDataID == sgd.SurveyGeoDataID
                   && Name == sgd.Name
                   && Value == sgd.Value;
        }
    }

    public class SurveyQuestionHidden
    {
        [Key]
        public int QuestionID { get; set; }
        public string QuestionResponse { get; set; }

        public bool Equals(SurveyQuestionHidden sqh)
        {
            return QuestionID == sqh.QuestionID
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
                   && Name == su.Name
                   && Value == su.Value;
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
                   && Name == svs.Name
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
            return questionId == srqd.questionId
                   && questionShortName == srqd.questionShortName
                   && questionOptionIdentifier == srqd.questionOptionIdentifier
                   && value == srqd.value
                   && isResponseAComment == srqd.isResponseAComment
                   && questionOptionTitle == srqd.questionOptionTitle;
        }
    }
}
