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
    public class DataItemConverterSurveyQuestion : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(SurveyQuestion);
        }

        public override bool CanRead => true;

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue,
            JsonSerializer serializer)
        {
            var value = (SurveyQuestion)existingValue ?? new SurveyQuestion();
            
            while (reader.Read())
            {
                if (reader.TokenType != JsonToken.PropertyName)
                {
                    continue;
                }
                var name = reader.Value.ToString();
                reader.Read();
                
               var  property = typeof(SurveyQuestion).GetProperty(name) ??
                               typeof(SurveyQuestion).GetProperties()
                                   .SingleOrDefault(p =>
                                   {
                                       return p.GetCustomAttributes(typeof(JsonPropertyAttribute), true)
                                           .Any(a => ((JsonPropertyAttribute)a).PropertyName == name);
                                   });
                


                if (property == null)
                {
                    continue;
                }
                if (property.PropertyType == typeof(DateTime))
                {
                    var propValDate = serializer.Deserialize(reader, typeof(string)).ToString();
                    var noTimeZone = propValDate.Replace(propValDate.Substring(propValDate.Length - 4), "");
                    var utc = noTimeZone + "Z";
                    var utcDate = DateTime.Parse(utc);
                    property.SetValue(value, utcDate, null);

                }else if (property.PropertyType == typeof(QuestionOptions[]))
                {
                    var questions = serializer.Deserialize(reader) as JObject;
                    if (questions != null)
                    {
                        Dictionary<string, object> results =
                            JsonConvert.DeserializeObject<Dictionary<string, object>>(questions.ToString());

                        var oList = new List<QuestionOptions>();

                        foreach (var questionObject in results.Values)
                        {
                            JObject questionJObject = JObject.Parse(questionObject.ToString());
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
                                property.SetValue(value, oList.ToArray(), null);
                            }
                        }
                    }
                    
                }
                else
                {
                    var propVal = serializer.Deserialize(reader, property.PropertyType);
                    property.SetValue(value, propVal, null);
                }

            }
            // Skip the , or } if we are at the end
            while(reader.TokenType != JsonToken.EndObject)
            {
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
