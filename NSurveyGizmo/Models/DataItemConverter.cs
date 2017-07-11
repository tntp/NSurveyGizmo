using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace NSurveyGizmo.Models
{
    public class DataItemConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(SurveyResponse);
        }

        public override bool CanRead => true;

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var value = (SurveyResponse) existingValue ?? new SurveyResponse
            {
                SurveyQuestions       = new List<SurveyQuestion>(),
                SurveyUrls            = new List<SurveyUrl>(),
                SurveyGeoDatas        = new List<SurveyGeoData>(),
                SurveyVariables       = new List<SurveyVariable>(),
                SurveyVariableShowns  = new List<SurveyVariableShown>(),
                SurveyQuestionHiddens = new List<SurveyQuestionHidden>(),
                SurveyQuestionOptions = new List<SurveyQuestionOption>(),
                SurveyQuestionMulties = new List<SurveyQuestionMulti>(),
                AllQuestions          = new Dictionary<int, string>()
            };

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
                var matchUrl = Regex.Match(input, @"\[url", RegexOptions.IgnoreCase);

                //GEO DATA
                var matchGeo = Regex.Match(input, @"\[variable\(""STANDARD_", RegexOptions.IgnoreCase);

                //VARIABLES SHOWN
                var matchVariables = Regex.Match(input, @"\[variable", RegexOptions.IgnoreCase);

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
                    var sq = new SurveyQuestion
                    {
                        id = index,
                        QuestionResponse = serializer.Deserialize<string>(reader)
                    };

                    value.SurveyQuestions.Add(sq);
                    value.AddQuestion(sq.id, sq.QuestionResponse);
                }
                else if (matchUrl.Success)
                {
                    var urlName = name.Substring(6, name.Length - 9);
                    var su = new SurveyUrl
                    {
                        Name = urlName,
                        Value = serializer.Deserialize<string>(reader)
                    };
                    value.SurveyUrls.Add(su);
                }
                else if (matchGeo.Success)
                {
                    var geoName = name.Substring(11, name.Length - 14);
                    var sgd = new SurveyGeoData
                    {
                        Name = geoName,
                        Value = serializer.Deserialize<string>(reader)
                    };
                    value.SurveyGeoDatas.Add(sgd);
                }
                else if (matchSingleVariable.Success)
                {
                    var index = int.Parse(name.Substring(10, name.IndexOf(')') - 10));
                    var sv = new SurveyVariable
                    {
                        SurveyVariableID = index,
                        Value            = serializer.Deserialize<string>(reader)
                    };
                    value.SurveyVariables.Add(sv);
                }
                else if (matchVariables.Success)
                {
                    var varName = name.Substring(11, name.Length - 14);
                    var svs = new SurveyVariableShown
                    {
                        Name  = varName,
                        Value = serializer.Deserialize<string>(reader)
                    };
                    value.SurveyVariableShowns.Add(svs);
                }
                else if (matchHiddenValue.Success)
                {
                    var index = int.Parse(name.Substring(10, name.IndexOf(')') - 10));
                    var sqh = new SurveyQuestionHidden
                    {
                        QuestionID       = index,
                        QuestionResponse = serializer.Deserialize<string>(reader)
                    };
                    value.SurveyQuestionHiddens.Add(sqh);
                }
                else if (matchMultiSelect.Success)
                {
                    //Multiple choice question selections
                    var nameArray    = name.Split(')');
                    var questionPart = nameArray[0];
                    var optionPart   = nameArray[1];
                    var index        = int.Parse(questionPart.Substring(10, questionPart.Length - 10));
                    var indexSub     = int.Parse(optionPart.Substring(9, optionPart.Length - 9));

                    var sqm = new SurveyQuestionMulti
                    {
                        OptionID         = indexSub,
                        QuestionID       = index,
                        QuestionResponse = serializer.Deserialize<string>(reader)
                    };

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
                    var nameArray    = name.Split(')');
                    var questionPart = nameArray[0];
                    var optionPart   = nameArray[1];
                    var index        = int.Parse(questionPart.Substring(10, questionPart.Length - 10));
                    var indexSub     = int.Parse(optionPart.Substring(10, 5));

                    var sqo = new SurveyQuestionOption
                    {
                        OptionID         = indexSub,
                        QuestionID       = index,
                        QuestionResponse = serializer.Deserialize<string>(reader)
                    };
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

        public override bool CanWrite => false;

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
