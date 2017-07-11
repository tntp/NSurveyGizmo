using System.ComponentModel.DataAnnotations;

namespace NSurveyGizmo.Models
{
    public class SurveyQuestionMulti
    {
        [Key]
        public int OptionID { get; set; }
        public int QuestionID { get; set; }
        public string QuestionResponse { get; set; }

        public bool Equals(SurveyQuestionMulti sqm)
        {
            return OptionID         == sqm.OptionID
                && QuestionID       == sqm.QuestionID
                && QuestionResponse == sqm.QuestionResponse;
        }
    }
}
