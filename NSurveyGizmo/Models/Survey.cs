using System;
using System.ComponentModel.DataAnnotations;

namespace NSurveyGizmo.Models
{
    public class Survey
    {
        [Key]
        public int id { get; set; }
        public string title { get; set; }
        public Links links { get; set; }
        public string status { get; set; }
        public DateTime created_on { get; set; }
    }

    public class Links
    {
        public string edit { get; set; }
        public string publish { get; set; }
    }
}
