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
            var value = (SurveyResponse) existingValue ?? new SurveyResponse();

            // Skip opening {
            reader.Read();
            while (reader.Read())
            {
                if (reader.TokenType != JsonToken.PropertyName)
                {
                    continue;
                }
                var name = reader.Value.ToString();
                reader.Read();

                var property = typeof(SurveyResponse).GetProperty(name) ??
                               typeof(SurveyResponse).GetProperties()
                                   .SingleOrDefault(p =>
                                   {
                                       return p.GetCustomAttributes(typeof(JsonPropertyAttribute), true)
                                           .Any(a => ((JsonPropertyAttribute) a).PropertyName == name);
                                   });

                if (property == null)
                {
                    continue;
                }
                if (property.PropertyType == typeof(DateTime))
                {

                    var propValDate = serializer.Deserialize(reader, typeof(String)).ToString();
                    var noTimeZone = propValDate.Replace(propValDate.Substring(propValDate.Length - 4), "");
                    var utc = noTimeZone + "Z";
                    var utcDate = DateTime.Parse(utc);
                    property.SetValue(value, utcDate, null);
                }
                else
                {
                    if (property.PropertyType == typeof(List<SurveyQuestion>))
                    {
                        var questions = serializer.Deserialize(reader) as JObject;
                        Dictionary<string, object> results = JsonConvert.DeserializeObject<Dictionary<string, object>>(questions.ToString());
                        
                        var qList = new List<SurveyQuestion>();
                        var oList = new List<QuestionOptions>();

                        foreach (var questionObject in results.Values)
                        {
                            JObject questionJObject = JObject.Parse(questionObject.ToString());
                            var q = new SurveyQuestion();
                            q.id = (int)questionJObject["id"];
                            q._type = (string)questionJObject["type"];
                            q.question = (string)questionJObject["question"];
                            q.section_id = (int)questionJObject["section_id"];
                            q.answer = (string)questionJObject["answer"];
                            q.shown = (bool)questionJObject["shown"];

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

                        property.SetValue(value, qList, null);
                    }
                    else
                    {
                        var propVal = serializer.Deserialize(reader, property.PropertyType) as JObject;
                        property.SetValue(value, propVal, null);
                    }
                   
                }
                
            }
            // Skip the , or } if we are at the end
            reader.Read();

            return value;
        }

        public override bool CanWrite => false;

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
