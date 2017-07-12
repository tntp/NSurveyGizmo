// Many thanks to Allan Horwitz for (most of) the code in this file
// http://stackoverflow.com/questions/14904328/how-to-read-json-response-for-elements-with-no-name-in-java-surveygizmo-survey

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
// ReSharper disable InconsistentNaming

namespace NSurveyGizmo
{
    public class PagedResult<T>
    {
        public bool result_ok { get; set; }
        public string total_count { get; set; }
        public int page { get; set; }
        public int total_pages { get; set; }
        public int results_per_page { get; set; }
        public T[] Data { get; set; }
    }

    public class Result<T>
    {
        public bool result_ok { get; set; }
        public T Data { get; set; }
    }

    public class Result
    {
        public int id { get; set; }
        public bool result_ok { get; set; }
    }

    [JsonObject]
    // Here is the magic: When you see this type, use this class to read it.
    // If you want, you can also define the JsonConverter by adding it to
    // a JsonSerializer, and parsing with that.
    [JsonConverter(typeof(DataItemConverter))]
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

    public class Survey
    {
        [Key]
        public int id { get; set; }
        public string title { get; set; }
        public Links links { get; set; }
        public string status { get; set; }
        public DateTime created_on { get; set; }
    }

    public class SurveyCampaign
    {
        [Key]
        public int id { get; set; }
        public string status { get; set; }
        public string name { get; set; }
        public string _type { get; set; }
        public string _subtype { get; set; }
        // there are other properties, but right now we only need the id, name, and status. we'll add these when we need them.
        // uri, SSL, tokenvariables, limit_responses, close_message, language, datecreated, datemodified
    }

    public class From
    {
        public string email { get; set; }
        public string name { get; set; }
    }

    public class Body
    {
        public string text { get; set; }
        public string html { get; set; }
    }

    public class EmailMessage
    {
        [Key]
        public int id { get; set; }
        public string _type { get; set; }
        public string _subtype { get; set; }
        public string messagetype { get; set; }
        public string medium { get; set; }
        public string invite_identity { get; set; }
        public string status { get; set; }
        public From from { get; set; }
        public string subject { get; set; }
        public Body body { get; set; }
        public string sfootercopy { get; set; }
        public DateTime datecreated { get; set; }
        public DateTime datemodified { get; set; }
    }

    public class Contact
    {
        [Key]
        public int id { get; set; }
        public string semailaddress { get; set; }
        public string sfirstname { get; set; }
        public string slastname { get; set; }
        public string sorganization { get; set; }
        public string sdivision { get; set; }
        public string sdepartment { get; set; }
        public string steam { get; set; }
        public string sgroup { get; set; }
        public string srole { get; set; }
        public string shomephone { get; set; }
        public string sfaxphone { get; set; }
        public string sbusinessphone { get; set; }
        public string smailingaddress { get; set; }
        public string smailingaddress2 { get; set; }
        public string smailingaddresscity { get; set; }
        public string smailingaddressstate { get; set; }
        public string scustomfield5 { get; set; }
        public string scustomfield7 { get; set; }
        public string scustomfield8 { get; set; }
        public string scustomfield9 { get; set; }
        public string scustomfield10 { get; set; }

        public string estatus { get; set; }
        // there are other properties. we'll add them when we need them.
    }

    public class Links
    {
        public string edit { get; set; }
        public string publish { get; set; }
    }

    public class SurveyQuestion
    {
        [Key]
        public int id { get; set; }
        public int surveypage { get; set; }
        public LocalizableString title { get; set; }
        public string _subtype { get; set; }
        public string _type { get; set; }
        public string shortName { get; set; }
        public string QuestionResponse { get; set; }
        public QuestionProperties properties { get; set; }
        public QuestionOptions[] options { get; set; }
    }

    public class QuestionProperties
    {
        public bool option_sort { get; set; }
        public bool required { get; set; }
        public bool hidden { get; set; }
        public string orientation { get; set; }
        public LocalizableString question_description { get; set; }
    }

    public class QuestionOptions
    {
        [Key]
        public int id { get; set; }
        public LocalizableString title { get; set; }
    }

    public class LocalizableString
    {
        public string English { get; set; }
    }

    public class SurveyUrl
    {
        [Key]
        public int SurveyUrlID { get; set; }

        public string Name { get; set; }
        public string Value { get; set; }
    }

    public class SurveyGeoData
    {
        [Key]
        public int SurveyGeoDataID { get; set; }

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

    public class SurveyQuestionHidden
    {
        [Key]
        public int QuestionID { get; set; }

        public string QuestionResponse { get; set; }
    }

    public class SurveyQuestionOption
    {
        [Key]
        public int OptionID { get; set; }
        public int id { get; set; }
        public string _type { get; set; }
        public int surveyID { get; set; }
        public int after { get; set; }
        public int surveypage { get; set; }
        public LocalizableString title { get; set; }
        public int QuestionID { get; set; }
        public string QuestionResponse { get; set; }
        public string value { get; set; }

    }
    public class SurveyResponseQuestionData
    {
        public int? questionId { get; set; }
        public string questionShortName { get; set; }
        public int? qestionOptionIdentifier { get; set; }
        public string value { get; set; }
        public bool isResonseAComment { get; set; }
        public string questionOptionTitle { get; set; }
    }

    public class SurveyQuestionMulti
    {
        [Key]
        public int OptionID { get; set; }

        public int QuestionID { get; set; }
        public string QuestionResponse { get; set; }
    }

    public class DataItemConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(SurveyResponse);
        }

        public override bool CanRead
        {
            get { return true; }
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue,
            JsonSerializer serializer)
        {
            var value = (SurveyResponse)existingValue;
            if (value == null)
            {
                value = new SurveyResponse();
                value.SurveyQuestions = new List<SurveyQuestion>();
                value.SurveyUrls = new List<SurveyUrl>();
                value.SurveyGeoDatas = new List<SurveyGeoData>();
                value.SurveyVariables = new List<SurveyVariable>();
                value.SurveyVariableShowns = new List<SurveyVariableShown>();
                value.SurveyQuestionHiddens = new List<SurveyQuestionHidden>();
                value.SurveyQuestionOptions = new List<SurveyQuestionOption>();
                value.SurveyQuestionMulties = new List<SurveyQuestionMulti>();
                value.AllQuestions = new Dictionary<int, string>();
            }

            // Skip opening {
            reader.Read();

            while (reader.TokenType == JsonToken.PropertyName)
            {
                var name = reader.Value.ToString();
                reader.Read();

                // Here is where you do your magic
                var input = name;

                //[question(1)]
                //[question(11)]
                //[question(111)]
                //[question(1234)]
                //[question(12345)]
                //[url(12345)]
                //[variable(12345)]
                //SINGLE ANSWER
                var matchSingleAnswer = Regex.Match(input,
                    @"\[(question|calc|comment)\(([0-9]{5}|[0-9]{4}|[0-9]{3}|[0-9]{2}|[0-9]{1})\)]",
                    RegexOptions.IgnoreCase);


                //SINGLE VARIABLE
                var matchSingleVariable = Regex.Match(input,
                    @"\[(variable)\(([0-9]{5}|[0-9]{4}|[0-9]{3}|[0-9]{2}|[0-9]{1})\)]",
                    RegexOptions.IgnoreCase);

                //URL
                var matchUrl = Regex.Match(input, @"\[url",
                    RegexOptions.IgnoreCase);

                //GEO DATA
                var matchGeo = Regex.Match(input, @"\[variable\(""STANDARD_",
                    RegexOptions.IgnoreCase);

                //VARIABLES SHOWN
                var matchVariables = Regex.Match(input, @"\[variable",
                    RegexOptions.IgnoreCase);

                //[question(1), option(\"1
                //[question(11), option(\"2
                //[question(111), option(\"1
                //[question(1234), option(\"1
                //[question(12345), option(\"1
                ////////////////////////////////////////////
                ////////The \ values are being removed.
                ////////////////////////////////////////////
                //OPTIONAL ANSWERS
                var myReg =
                    @"\[(question|url|variable|calc|comment)\(([0-9]{5}|[0-9]{4}|[0-9]{3}|[0-9]{2}|[0-9]{1})\),\ option\(""[0-9]";
                var matchOption = Regex.Match(input, myReg,
                    RegexOptions.IgnoreCase);

                //[question(1), option(1
                //[question(11), option(2
                //[question(111), option(1
                //[question(1234), option(1
                //[question(12345), option(1
                //MULTIPLE CHOICE
                var matchMultiSelect = Regex.Match(input,
                    @"\[question\(([0-9]{5}|[0-9]{4}|[0-9]{3}|[0-9]{2}|[0-9]{1})\),\ option\([0-9]",
                    RegexOptions.IgnoreCase);

                //[question(1), option(0)
                //[question(11), option(0)
                //[question(111), option(0)
                //[question(1234), option(0)
                //[question(12345), option(0)
                //HIDDEN
                var matchHiddenValue = Regex.Match(input,
                    @"\[question\(([0-9]{5}|[0-9]{4}|[0-9]{3}|[0-9]{2}|[0-9]{1})\),\ option\(0\)",
                    RegexOptions.IgnoreCase);


                if (matchSingleAnswer.Success)
                {
                    var index = int.Parse(name.Substring(10, name.IndexOf(')') - 10));
                    var sq = new SurveyQuestion();
                    sq.id = index;
                    sq.QuestionResponse = serializer.Deserialize<string>(reader);

                    value.SurveyQuestions.Add(sq);
                    value.AddQuestion(sq.id, sq.QuestionResponse);
                }
                else if (matchUrl.Success)
                {
                    var urlName = name.Substring(6, name.Length - 9);
                    var su = new SurveyUrl();
                    su.Name = urlName;
                    su.Value = serializer.Deserialize<string>(reader);
                    value.SurveyUrls.Add(su);
                }
                else if (matchGeo.Success)
                {
                    var geoName = name.Substring(11, name.Length - 14);
                    var sgd = new SurveyGeoData();
                    sgd.Name = geoName;
                    sgd.Value = serializer.Deserialize<string>(reader);
                    value.SurveyGeoDatas.Add(sgd);
                }
                else if (matchSingleVariable.Success)
                {
                    var index = int.Parse(name.Substring(10, name.IndexOf(')') - 10));
                    var sv = new SurveyVariable();
                    sv.SurveyVariableID = index;
                    sv.Value = serializer.Deserialize<string>(reader);
                    value.SurveyVariables.Add(sv);
                }
                else if (matchVariables.Success)
                {
                    var varName = name.Substring(11, name.Length - 14);
                    var svs = new SurveyVariableShown();
                    svs.Name = varName;
                    svs.Value = serializer.Deserialize<string>(reader);
                    value.SurveyVariableShowns.Add(svs);
                }
                else if (matchHiddenValue.Success)
                {
                    var index = int.Parse(name.Substring(10, name.IndexOf(')') - 10));
                    var sqh = new SurveyQuestionHidden();
                    sqh.QuestionID = index;
                    sqh.QuestionResponse = serializer.Deserialize<string>(reader);
                    value.SurveyQuestionHiddens.Add(sqh);
                }
                else if (matchMultiSelect.Success)
                {
                    //Multiple choice question selections
                    var nameArray = name.Split(')');
                    var questionPart = nameArray[0];
                    var optionPart = nameArray[1];
                    var index = int.Parse(questionPart.Substring(10, questionPart.Length - 10));
                    var indexSub = int.Parse(optionPart.Substring(9, optionPart.Length - 9));

                    var sqm = new SurveyQuestionMulti();
                    sqm.OptionID = indexSub;
                    sqm.QuestionID = index;
                    sqm.QuestionResponse = serializer.Deserialize<string>(reader);

                    value.SurveyQuestionMulties.Add(sqm);
                    value.AddQuestion(sqm.QuestionID, sqm.QuestionResponse);

                    //NEED TO ADD A BASE QUESTION TO POINT TO ALL THE MULTI
                    //SurveyQuestion sq = new SurveyQuestion();
                    //sq.QuestionID = sqm.QuestionID;
                    //sq.QuestionResponse = "";
                    //value.SurveyQuestions.Add(sq);
                }
                else if (matchOption.Success)
                {
                    //Optional text value for a given question
                    var nameArray = name.Split(')');
                    var questionPart = nameArray[0];
                    var optionPart = nameArray[1];
                    var index = int.Parse(questionPart.Substring(10, questionPart.Length - 10));
                    var indexSub = int.Parse(optionPart.Substring(10, 5));

                    var sqo = new SurveyQuestionOption();
                    sqo.OptionID = indexSub;
                    sqo.QuestionID = index;
                    sqo.QuestionResponse = serializer.Deserialize<string>(reader);
                    value.SurveyQuestionOptions.Add(sqo);
                    value.AddQuestion(sqo.QuestionID, sqo.QuestionResponse);
                }
                else
                {
                    var property = typeof(SurveyResponse).GetProperty(name);
                    if (property != null)
                        property.SetValue(value, serializer.Deserialize(reader, property.PropertyType), null);
                }

                // Skip the , or } if we are at the end
                reader.Read();
            }

            return value;
        }

        public override bool CanWrite
        {
            get { return false; }
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}

