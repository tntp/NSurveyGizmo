using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NSurveyGizmo.Models.v5
{
    public class EmailMessage
    {
        [Key]
        public int id { get; set; }
        public string subtype { get; set; }
        public string message_type { get; set; }
        public string medium { get; set; }
        public string invite_identity { get; set; }
        public string status { get; set; }
        public From from { get; set; }
        public string subject { get; set; }
        public Body body { get; set; }
        public string footer { get; set; }
        public DateTime date_created { get; set; }
        public DateTime date_modified { get; set; }

        public bool Equals(EmailMessage msg)
        {
            return id == msg.id
                   && subtype == msg.subtype
                   && message_type == msg.message_type
                   && medium == msg.medium
                   && invite_identity == msg.invite_identity
                   && status == msg.status
                   && subject == msg.subject
                   && footer == msg.footer
                   && date_created == msg.date_created
                   && date_modified == msg.date_modified
                   && from.Equals(msg.from)
                   && body.Equals(msg.body);
        }
    }

    public class From
    {
        public string email { get; set; }
        public string name { get; set; }

        public bool Equals(From f)
        {
            return email == f.email && name == f.name;
        }
    }

    public class Body
    {
        public string text { get; set; }
        public string html { get; set; }

        public bool Equals(Body b)
        {
            return text == b.text && html == b.html;
        }
    }
}
