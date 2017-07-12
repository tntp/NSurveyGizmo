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

        // TODO: Update Equals() method to account for new properties
        public int id { get; set; }
        public string _type { get; set; }


        public bool Equals(SurveyQuestionOption sqo)
        {
            return OptionID         == sqo.OptionID
                && surveyID         == sqo.surveyID
                && after            == sqo.after
                && surveypage       == sqo.surveypage
                && QuestionID       == sqo.QuestionID
                && QuestionResponse == sqo.QuestionResponse
                && value            == sqo.value
                && title.Equals(sqo.title);
        }
    }
}
