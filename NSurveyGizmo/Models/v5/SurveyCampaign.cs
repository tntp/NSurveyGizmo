using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NSurveyGizmo.Models.v5
{
    public class SurveyCampaign
    {
        [Key]
        public int id { get; set; }
        public string status { get; set; }
        public string name { get; set; }
        public string type { get; set; }
        public string subtype { get; set; }

        public bool Equals(SurveyCampaign sc)
        {
            return id == sc.id
                   && status == sc.status
                   && name == sc.name
                   && type == sc.type
                   && subtype == sc.subtype;
        }
    }
}
