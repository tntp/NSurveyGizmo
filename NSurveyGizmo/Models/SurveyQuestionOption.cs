using System.ComponentModel.DataAnnotations;

namespace NSurveyGizmo.Models
{
    public class SurveyQuestionOption
    {
        [Key]
        public int OptionID { get; set; }
        public int surveyID { get; set; }
        public int after { get; set; }
        public int surveypage { get; set; }
        public LocalizableString title { get; set; }
        public int QuestionID { get; set; }
        public string QuestionResponse { get; set; }
        public string value { get; set; }

    }
}
