﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace NSurveyGizmo.Models.v5
{
    [JsonObject, JsonConverter(typeof(DataItemConverter))]
    public class SurveyQuestion
    {
        [Key]
       
        public int id { get; set; }
        public int page { get; set; }
        public LocalizableString title { get; set; }
        public string type { get; set; }
        public string shortName { get; set; }
        public string value { get; set; }
        public string question { get; set; }
        public string answer { get; set; }
        public string shown { get; set; }
        public string section_id { get; set; }
        public QuestionProperties properties { get; set; }
        public QuestionOptions[] options { get; set; }

        public bool Equals(SurveyQuestion sq)
        {
            return id == sq.id
                   && type == sq.type
                   && shortName == sq.shortName
                   && question == sq.question
                   && title.Equals(sq.title)
                   && value.Equals(sq.value)
                   && properties.Equals(sq.properties)
                   && options.SequenceEqual(sq.options);
        }
    }

    public class QuestionProperties
    {
        public bool option_sort { get; set; }
        public bool required { get; set; }
        public bool hidden { get; set; }
        public string orientation { get; set; }
        public Models.LocalizableString question_description { get; set; }

        public bool Equals(QuestionProperties qp)
        {
            return option_sort == qp.option_sort
                   && required == qp.required
                   && hidden == qp.hidden
                   && orientation == qp.orientation
                   && question_description.Equals(qp.question_description);
        }
    }

    public class QuestionOptions
    {
        [Key]
        public int id { get; set; }
        public string option { get; set; }
        public string answer { get; set; }
        public LocalizableString title { get; set; }

        public bool Equals(QuestionOptions qo)
        {
            return id == qo.id && title.Equals(qo.title);
        }
    }
}