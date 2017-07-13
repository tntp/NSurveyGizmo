using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NSurveyGizmo.Models.v5
{
    public class SurveyQuestionOption
    {
        [Key]
        public int id { get; set; }
        public int OptionID { get; set; }
        public int surveyID { get; set; }
        public int after { get; set; }
        public int surveypage { get; set; }
        public LocalizableString title { get; set; }
        public int QuestionID { get; set; }
        public string QuestionResponse { get; set; }
        public string value { get; set; }

        public bool Equals(SurveyQuestionOption sqo)
        {
            return id == sqo.id
                   && surveyID == sqo.surveyID
                   && after == sqo.after
                   && surveypage == sqo.surveypage
                   && QuestionID == sqo.QuestionID
                   && QuestionResponse == sqo.QuestionResponse
                   && value == sqo.value
                   && title.Equals(sqo.title);
        }
    }
}
