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
                else if (property.PropertyType == typeof(List<SurveyUrl>))
                {
                    var urls = serializer.Deserialize(reader) as JObject;
                    if (urls != null)
                    {
                        Dictionary<string, object> results =
                            JsonConvert.DeserializeObject<Dictionary<string, object>>(urls?.ToString());

                        foreach (var urlParam in results.Values)
                        {
                            JObject urlJObject = JObject.Parse(urlParam.ToString());
                            var q = new SurveyUrl()
                            {
                                Name = (string) urlJObject["key"],
                                Value = (string) urlJObject["value"]
                            };
                            value.SurveyUrls.Add(q);
                        }
                    }
                }
                else if (property.PropertyType == typeof(List<SurveyGeoData>))
                {
                    var dataQuality = serializer.Deserialize<SurveyGeoData[]>(reader);
                    foreach (var dq in dataQuality)
                    {
                        if (oldv4SurveyGeoData.ContainsKey(dq.Name))
                        {
                            var newgeo = new SurveyGeoData()
                            {
                                Name = dq.Name,
                                Value = dq.Value
                            };
                            value.SurveyGeoDatas.Add(newgeo);
                        }
                    }
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
                            }

                            q.options = oList.ToArray();
                        }
                        if (questionJObject["subquestions"] != null)
                        {
                            var subquestions = (JObject)questionJObject["subquestions"];
                            Dictionary<string, object> subquestionsObjectDictionary =
                                JsonConvert.DeserializeObject<Dictionary<string, object>>(subquestions?.ToString());
                            foreach (var subquestionsObject in subquestionsObjectDictionary.Values)
                            {
                                JObject questionJObjectagain = JObject.Parse(subquestionsObject.ToString());
                                var qsub = new SurveyQuestion
                                {
                                    id = (int)questionJObjectagain["id"],
                                    _subtype = (string)questionJObjectagain["type"],
                                    question = (string)questionJObjectagain["question"],
                                    QuestionResponse = (string)questionJObjectagain["answer"],
                                    master_question_id = (int?)questionJObjectagain["parent"] ?? 0
                                };
                                if (questionJObjectagain["answer_id"] != null)
                                {
                                    qsub.answer_id = (int)questionJObjectagain["answer_id"];
                                    var sv = new SurveyVariable()
                                    {
                                        SurveyVariableID = qsub.id,
                                        Value = qsub.answer_id.ToString()
                                    };
                                    value.SurveyVariables.Add(sv);
                                }
                                if (q.shown == false || q._type == "hidden")
                                {
                                    var sqh = new SurveyQuestionHidden
                                    {
                                        QuestionID = qsub.id,
                                        QuestionResponse = qsub.QuestionResponse ?? ""
                                    };
                                    value.SurveyQuestionHiddens.Add(sqh);
                                }
                                else if (q.shown)
                                {
                                    var svs = new SurveyVariableShown()
                                    {
                                        Name = qsub.id + "-shown",
                                        Value = "1"//meaning true
                                    };
                                    value.SurveyVariableShowns.Add(svs);
                                }
                                if (questionJObjectagain["options"] != null)
                                {
                                    Dictionary<string, object> subquestionOptions =
                                        JsonConvert.DeserializeObject<Dictionary<string, object>>(
                                            questionJObjectagain["options"]
                                                .ToString());
                                    foreach (var optionObject in subquestionOptions.Values)
                                    {
                                        JObject optionJObject = JObject.Parse(optionObject.ToString());
                                        var o = new QuestionOptions
                                        {
                                            id = (int)optionJObject["id"],
                                            answer = (string)optionJObject["answer"],
                                            option = (string)optionJObject["option"]
                                        };
                                        oList.Add(o);

                                        var soObject = new SurveyQuestionOption
                                        {
                                            id = (int)optionJObject["id"],
                                            OptionID = (int)optionJObject["id"],
                                            QuestionResponse = (string)optionJObject["answer"],
                                            surveyID = 0,
                                            title = new LocalizableString { English = (string)optionJObject["option"] },
                                            value = (string)optionJObject["option"],
                                            QuestionID = q.id
                                        };
                                        questionOptionAnser.QuestionResponse = soObject.QuestionResponse;
                                        var sqmObject = new SurveyQuestionMulti()
                                        {
                                            OptionID = (int)optionJObject["id"],
                                            QuestionResponse = (string)optionJObject["answer"],
                                            QuestionID = qsub.id
                                        };

                                        soList.Add(soObject);
                                        value.SurveyQuestionMulties.Add(sqmObject);
                                    }

                                    q.options = oList.ToArray();
                                }
                                value.AddQuestion(qsub.id,
                                    qsub.QuestionResponse ?? questionOptionAnser.QuestionResponse);
                                qList.Add(qsub);
                            }
                        }

                        value.AddQuestion(q.id, q.QuestionResponse ?? questionOptionAnser.QuestionResponse);
                        qList.Add(q);
                    }
                    //value.SurveyQuestionOptions = soList; the v4 return value was 0 for this field so im not going to add them for v5
                    var qListWOQuestionMulti = qList.Where(x => x.options == null).ToList();
                    property.SetValue(value, qListWOQuestionMulti, null);

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
