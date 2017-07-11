// ReSharper disable NonReadonlyMemberInGetHashCode
using System;
using System.ComponentModel.DataAnnotations;

namespace NSurveyGizmo.Models
{
    public class EmailMessage
    {
        [Key]
        public int id { get; set; }
        public string _type { get; set; }
        public string _subtype { get; set; }
        public string messagetype { get; set; }
        public string medium { get; set; }
        public string invite_identity { get; set; }
        public string status { get; set; }
        public From from { get; set; }
        public string subject { get; set; }
        public Body body { get; set; }
        public string sfootercopy { get; set; }
        public DateTime datecreated { get; set; }
        public DateTime datemodified { get; set; }

        public bool Equals(EmailMessage msg)
        {
            return id              == msg.id
                && _type           == msg._type
                && _subtype        == msg._subtype
                && messagetype     == msg.messagetype
                && medium          == msg.medium
                && invite_identity == msg.invite_identity
                && status          == msg.status
                && subject         == msg.subject
                && sfootercopy     == msg.sfootercopy
                && datecreated     == msg.datecreated
                && datemodified    == msg.datemodified
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
