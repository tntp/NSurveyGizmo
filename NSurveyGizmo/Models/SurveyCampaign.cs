using System.ComponentModel.DataAnnotations;

namespace NSurveyGizmo.Models
{
    // there are other properties, but right now we only need the id, name, and status. we'll add these when we need them.
    // uri, SSL, tokenvariables, limit_responses, close_message, language, datecreated, datemodified

    public class SurveyCampaign
    {
        [Key]
        public int id { get; set; }
        public string status { get; set; }
        public string name { get; set; }
        public string _type { get; set; }
        public string _subtype { get; set; }
    }
}
