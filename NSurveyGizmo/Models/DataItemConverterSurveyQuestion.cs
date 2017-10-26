using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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
            // Skip opening {
            reader.Read();
            while (reader.TokenType == JsonToken.PropertyName)
            {
                var name = reader.Value.ToString();
                reader.Read();
               
                   var property = typeof(SurveyQuestion).GetProperty(name) ??
                               typeof(SurveyQuestion).GetProperties()
                                   .SingleOrDefault(p =>
                                   {
                                       return p.GetCustomAttributes(typeof(JsonPropertyAttribute), true)
                                           .Any(a => ((JsonPropertyAttribute)a).PropertyName == name);
                                   });

                if (property == null)
                {
                    reader.Read();
                    continue;
                }
                if (name == "varname")//varname returns either an object, a string array, or an empty array
                {
                    try
                    {
                        var propVal = serializer.Deserialize(reader, property.PropertyType);
                       
                        if (propVal != null)
                        {
                            property.SetValue(value, propVal, null);
                        }
                    }
                    catch (Exception e)
                    {
                        try
                        {
                            var propVal = serializer.Deserialize(reader, typeof(string[]));
                            
                            var strArr = (string[]) propVal;
                            if (propVal != null)
                            {
                                var dict = new Dictionary<string, string>();
                                dict.Add(strArr[0], strArr[0]);
                                property.SetValue(value, dict, null);
                            }
                        }
                        catch (Exception ex)
                        {
                            reader.Skip();
                        }
                    }
                }else if (name == "properties")
                {
                    var questionProperties = serializer.Deserialize(reader) as JObject;

                    if (questionProperties != null) { 

                        var qCodeField = new LocalizableString();
                        if (value.varname != null && value.varname.Count > 0 && value.varname.Values.Any(x => !string.IsNullOrEmpty(x)) && value.sub_questions == null)
                        {
                            qCodeField.English = value.varname.Values.First(x => !string.IsNullOrEmpty(x));
                        }
                        else
                        {
                            qCodeField.English = (string) (questionProperties["question_description"]?["English"] ?? "");
                        }
                        var p = new QuestionProperties()
                        {
                            option_sort = (bool) (questionProperties["option_sort"] ?? false),
                            required = (bool) (questionProperties["required"] ?? false),
                            hidden = (bool) (questionProperties["hidden"] ?? true),
                            orientation = (string) (questionProperties["orientation"] ?? ""),
                            question_description = qCodeField
                        };
                        property.SetValue(value, p, null);
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
