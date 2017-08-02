using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using NSurveyGizmo.Models;

namespace NSurveyGizmo.Models
{
    public class DataItemConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(SurveyResponse);
        }

        public override bool CanRead => true;

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue,
            JsonSerializer serializer)
        {
            var oldv4SurveyGeoData = new Dictionary<string, string>()
            {
                {"ip_address", "STANDARD_IP"},
                {"longitude", "STANDARD_LONG"},
                {"latitude", "STANDARD_LAT"},
                {"country", "STANDARD_GEOCOUNTRY"},
                {"city", "STANDARD_GEOCITY"},
                {"region", "STANDARD_GEOREGION"},
                {"postal", "STANDARD_GEOPOSTAL"},
                {"response_time", "STANDARD_RESPONSETIME"},
                {"comments", "STANDARD_COMMENTS"},
                {"dma", "STANDARD_GEODMA"},
                {"user_agent", "STANDARD_USERAGENT"},
                {"referer", "STANDARD_REFERER"}
            };
            var value = (SurveyResponse) existingValue ?? new SurveyResponse();
            // Skip opening {
            reader.Read();
            while (reader.TokenType == JsonToken.PropertyName)
            {
                
                var name = reader.Value.ToString();
                reader.Read();

         
                PropertyInfo property;
                if (oldv4SurveyGeoData.ContainsKey(name))
                {
                    property = typeof(SurveyGeoData).GetProperty("Value");
                }
                else
                {
                    property = typeof(SurveyResponse).GetProperty(name) ??
                               typeof(SurveyResponse).GetProperties()
                                   .SingleOrDefault(p =>
                                   {
                                       return p.GetCustomAttributes(typeof(JsonPropertyAttribute), true)
                                           .Any(a => ((JsonPropertyAttribute) a).PropertyName == name);
                                   });
                }


                if (property == null)
                {
                    reader.Read();
                    continue;
                }
                if (property.PropertyType == typeof(DateTime))
                {
                    var propValDate = serializer.Deserialize(reader, typeof(string)).ToString();
                    var noTimeZone = propValDate.Replace(propValDate.Substring(propValDate.Length - 4), "");
                    var utc = noTimeZone + "Z";
                    var utcDate = DateTime.Parse(utc);
                    property.SetValue(value, utcDate, null);

                }
                else if (oldv4SurveyGeoData.ContainsKey(name))
                {
                    string oldName = oldv4SurveyGeoData.FirstOrDefault(x => x.Key == name).Value;
                    var geoName = oldName;
                    var sgd = new SurveyGeoData
                    {
                        Name = geoName,
                        Value = serializer.Deserialize<string>(reader)
                    };
                    value.SurveyGeoDatas.Add(sgd);

                }
                else if(name == "url_variables")
                {
                    var urlParams = serializer.Deserialize(reader) as JObject;
                    try
                    {
                        Dictionary<string, object> results =
                            JsonConvert.DeserializeObject<Dictionary<string, object>>(urlParams?.ToString());
                    }
                    catch(Exception e)
                    {
                        try
                        {
                            Dictionary<string, string> results2 =
                                JsonConvert.DeserializeObject<Dictionary<string, string>>(urlParams?.ToString());
                        }
                        catch (Exception ef)
                        {
                         reader.Skip();   
                        }
                    }
                    
                }
                else if (name == "data_quality")
                {
                    var dataQual = serializer.Deserialize(reader) as JObject;
                    try
                    {
                        Dictionary<string, object> results =
                            JsonConvert.DeserializeObject<Dictionary<string, object>>(dataQual?.ToString());
                    }
                    catch (Exception e)
                    {
                        try
                        {
                            Dictionary<string, string> results2 =
                                JsonConvert.DeserializeObject<Dictionary<string, string>>(dataQual?.ToString());
                        }
                        catch (Exception ef)
                        {
                            reader.Skip();
                        }
                    }

                }
                else if (property.PropertyType == typeof(List<SurveyQuestion>))
                {
                    var questionOptionAnser = new SurveyQuestionOption();
                    var questions = serializer.Deserialize(reader) as JObject;
                    Dictionary<string, object> results =
                        JsonConvert.DeserializeObject<Dictionary<string, object>>(questions?.ToString());

                    var qList = new List<SurveyQuestion>();
                    var oList = new List<QuestionOptions>();
                    var soList = new List<SurveyQuestionOption>();

                    foreach (var questionObject in results.Values)
                    {
                        JObject questionJObject = JObject.Parse(questionObject.ToString());
                        var q = new SurveyQuestion
                        {
                            id = (int) questionJObject["id"],
                            _type = (string) questionJObject["base_type"],
                            _subtype = (string)questionJObject["type"],
                            question = (string) questionJObject["question"],
                            section_id = (int) questionJObject["section_id"],
                            QuestionResponse = (string) questionJObject["answer"]

                        };
                        if (questionJObject["answer_id"] != null)
                        {
                            q.answer_id = (int) questionJObject["answer_id"];
                            var sv = new SurveyVariable()
                            {
                                SurveyVariableID = q.id,
                                Value = q.answer_id.ToString()
                            };
                            value.SurveyVariables.Add(sv);
                        }
                        q.shown = (bool) questionJObject["shown"];
                        if (q.shown == false || q._type == "hidden")
                        {
                            var sqh = new SurveyQuestionHidden
                            {
                                QuestionID = q.id,
                                QuestionResponse = q.QuestionResponse ?? ""
                            };
                            value.SurveyQuestionHiddens.Add(sqh);
                        }else if (q.shown)
                        {
                            var svs = new SurveyVariableShown()
                            {
                                Name = q.id + "-shown",
                                Value = "1"//meaning true
                            };
                            value.SurveyVariableShowns.Add(svs);
                        }
                        if (questionJObject["options"] != null)
                        {
                            Dictionary<string, object> questionOptions =
                                JsonConvert.DeserializeObject<Dictionary<string, object>>(questionJObject["options"]
                                    .ToString());
                            foreach (var optionObject in questionOptions.Values)
                            {
                                JObject optionJObject = JObject.Parse(optionObject.ToString());
                                var qoptionquestion = new SurveyQuestion
                                {
                                    id = (int)optionJObject["id"],
                                    _subtype = (string)optionJObject["type"],
                                    question = (string)optionJObject["question"],
                                    QuestionResponse = (string)optionJObject["answer"],
                                    master_question_id = q.id
                                };
                                if (optionJObject["answer_id"] != null)
                                {
                                    qoptionquestion.answer_id = (int)optionJObject["answer_id"];
                                    var sv = new SurveyVariable()
                                    {
                                        SurveyVariableID = qoptionquestion.id,
                                        Value = qoptionquestion.answer_id.ToString()
                                    };
                                    value.SurveyVariables.Add(sv);
                                }
                                if (q.shown == false || q._type == "hidden")
                                {
                                    var sqh = new SurveyQuestionHidden
                                    {
                                        QuestionID = qoptionquestion.id,
                                        QuestionResponse = qoptionquestion.QuestionResponse ?? ""
                                    };
                                    value.SurveyQuestionHiddens.Add(sqh);
                                }
                                else if (q.shown)
                                {
                                    var svs = new SurveyVariableShown()
                                    {
                                        Name = qoptionquestion.id + "-shown",
                                        Value = "1"//meaning true
                                    };
                                    value.SurveyVariableShowns.Add(svs);
                                }
                                var o = new QuestionOptions
                                {
                                    id = (int) optionJObject["id"],
                                    answer = (string) optionJObject["answer"],
                                    option = (string) optionJObject["option"]
                                };
                                oList.Add(o);

                                var soObject = new SurveyQuestionOption
                                {
                                    id = (int) optionJObject["id"],
                                    OptionID = (int) optionJObject["id"],
                                    QuestionResponse = (string) optionJObject["answer"],
                                    surveyID = 0,
                                    title = new LocalizableString {English = (string) optionJObject["option"]},
                                    value = (string) optionJObject["option"],
                                    QuestionID = q.id
                                };
                                questionOptionAnser.QuestionResponse = soObject.QuestionResponse;
                                var sqmObject = new SurveyQuestionMulti()
                                {
                                    OptionID = (int)optionJObject["id"],
                                    QuestionResponse = (string)optionJObject["answer"],
                                    QuestionID = q.id
                                };

                                soList.Add(soObject);
                                value.SurveyQuestionMulties.Add(sqmObject);
                                value.AddQuestion(qoptionquestion.id,
                                    qoptionquestion.QuestionResponse ?? questionOptionAnser.QuestionResponse);
                                qList.Add(qoptionquestion);
                            }
                            q.options = oList.ToArray();
                        }
                        value.AddQuestion(q.id, q.QuestionResponse ?? questionOptionAnser.QuestionResponse);
                        qList.Add(q);
                    }
                    //value.SurveyQuestionOptions = soList; the v4 return value was 0 for this field so im not going to add them for v5
                    var qListWoQuestionMulti = qList.Where(x => x.options == null).ToList();
                    property.SetValue(value, qListWoQuestionMulti, null);

                }
                else
                {
                    var propVal = serializer.Deserialize(reader, property.PropertyType);
                    property.SetValue(value, propVal, null);
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
