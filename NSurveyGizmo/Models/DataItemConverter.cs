﻿using Newtonsoft.Json;
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
            return objectType == typeof(Result);
        }

        public override bool CanRead => true;

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue,
            JsonSerializer serializer)
        {
            //taking the new response format and jamming it into the old model to keep everything the same
            var oldv4SurveyGeoData = new Dictionary<string, string>()
            {
                {"ip_address", "STANDARD_IP"},
                {"longitude", "STANDARD_LONG"},
                {"latitude", "STANDARD_LAT"},
                {"country", "STANDARD_GEOCOUNTRY"},
                {"city", "STANDARD_GEOCITY"},
                {"region", "STANDARD_GEOREGION"},
                {"response_time", "STANDARD_RESPONSETIME"},
                {"comments", "STANDARD_COMMENTS"}
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
                    var urlName = name;
                    var urlParamArray = serializer.Deserialize<string[]>(reader);
                    foreach (var param in urlParamArray)
                    {
                        var su = new SurveyUrl
                        {
                            Name = urlName,
                            Value = param
                        };
                        value.SurveyUrls.Add(su);
                    }

                }
                else if (property.PropertyType == typeof(List<SurveyGeoData>))
                {
                    
                    var dataQuality = serializer.Deserialize<SurveyGeoData[]>(reader);

                    value.SurveyGeoDatas.AddRange(dataQuality);
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
                            _type = (string) questionJObject["type"],
                            question = (string) questionJObject["question"],
                            section_id = (int) questionJObject["section_id"],
                            QuestionResponse = (string) questionJObject["answer"]
                        };
                        if (questionJObject["answer_id"] != null)
                        {
                            q.answer_id = (int) questionJObject["answer_id"];
                        }
                        q.shown = (bool) questionJObject["shown"];

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
                                q.answer_id = (int)optionJObject["id"];
                                q.QuestionResponse = (string)optionJObject["answer"];
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

                                soList.Add(soObject);
                            }

                            q.options = oList.ToArray();
                        }
                        value.AddQuestion(q.id, q.QuestionResponse);
                        qList.Add(q);
                    }
                    value.SurveyQuestionOptions = soList;
                    property.SetValue(value, qList, null);

                }
                else if (property.PropertyType == typeof(SurveyQuestion))
                {
                    var questions = serializer.Deserialize(reader) as JObject;
                    Dictionary<string, object> results =
                        JsonConvert.DeserializeObject<Dictionary<string, object>>(questions.ToString());

                    var qList = new List<SurveyQuestion>();
                    var oList = new List<QuestionOptions>();

                    foreach (var questionObject in results.Values)
                    {
                        JObject questionJObject = JObject.Parse(questionObject.ToString());
                        var q = new SurveyQuestion();
                        q.id = (int) questionJObject["id"];
                        q._type = (string) questionJObject["type"];
                        q.question = (string) questionJObject["question"];
                        q.section_id = (int) questionJObject["section_id"];
                        q.QuestionResponse = (string) questionJObject["answer"];
                        q.shown = (bool) questionJObject["shown"];

                        if (questionJObject["options"] != null)
                        {
                            Dictionary<string, object> questionOptions =
                                JsonConvert.DeserializeObject<Dictionary<string, object>>(questionJObject["options"]
                                    .ToString());
                            foreach (var optionObject in questionOptions.Values)
                            {
                                JObject optionJObject = JObject.Parse(optionObject.ToString());
                                var o = new QuestionOptions();
                                o.id = (int) optionJObject["id"];
                                o.answer = (string) optionJObject["answer"];
                                o.option = (string) optionJObject["option"];
                                oList.Add(o);
                            }

                            q.options = oList.ToArray();
                        }
                        qList.Add(q);
                    }
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