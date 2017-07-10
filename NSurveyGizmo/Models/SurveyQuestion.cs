using System.ComponentModel.DataAnnotations;

namespace NSurveyGizmo.Models
{
    public class SurveyQuestion
    {
        [Key]
        public int id { get; set; }
        public int surveypage { get; set; }
        public LocalizableString title { get; set; }
        public string _subtype { get; set; }
        public string _type { get; set; }
        public string QuestionResponse { get; set; }
        public QuestionProperties properties { get; set; }
        public QuestionOptions[] options { get; set; }
    }

    public class QuestionProperties
    {
        public bool option_sort { get; set; }
        public bool required { get; set; }
        public bool hidden { get; set; }
        public string orientation { get; set; }
        public LocalizableString question_description { get; set; }
    }

    public class QuestionOptions
    {
        [Key]
        public int id { get; set; }
        public LocalizableString title { get; set; }
    }
}
