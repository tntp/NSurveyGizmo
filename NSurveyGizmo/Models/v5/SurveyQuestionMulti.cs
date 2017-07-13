using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NSurveyGizmo.Models.v5
{
    public class SurveyQuestionMulti
    {
        [Key]
        public int OptionID { get; set; }
        public int QuestionID { get; set; }
        public string QuestionResponse { get; set; }

        public bool Equals(SurveyQuestionMulti sqm)
        {
            return OptionID == sqm.OptionID
                   && QuestionID == sqm.QuestionID
                   && QuestionResponse == sqm.QuestionResponse;
        }
    }
}
