using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NSurveyGizmo.Models.v5
{
    public class Survey
    {
        [Key]
        public int id { get; set; }

        public string title { get; set; }
        public Links links { get; set; }
        public string status { get; set; }
        public DateTime created_on { get; set; }

        public bool Equals(Survey s)
        {
            return id == s.id
                   && title == s.title
                   && status == s.status
                   && created_on == s.created_on
                   && links.Equals(s.links);
        }
    }

    public class Links
    {
        public string edit { get; set; }
        public string publish { get; set; }

        public bool Equals(Links l)
        {
            return edit == l.edit && publish == l.publish;
        }
    }
}