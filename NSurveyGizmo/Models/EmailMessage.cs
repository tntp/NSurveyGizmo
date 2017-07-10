﻿// ReSharper disable NonReadonlyMemberInGetHashCode
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

        public override bool Equals(object obj)
        {
            return Equals(obj as EmailMessage);
        }

        public bool Equals(EmailMessage msg)
        {
            return id              == msg.id
                && _type           == msg._type
                && _subtype        == msg._subtype
                && messagetype     == msg.messagetype
                && medium          == msg.medium
                && invite_identity == msg.invite_identity
                && status          == msg.status
                && from            == msg.from
                && subject         == msg.subject
                && body            == msg.body
                && sfootercopy     == msg.sfootercopy
                && datecreated     == msg.datecreated
                && datemodified    == msg.datemodified;
        }

        public override int GetHashCode()
        {
            return new
            {
                id,
                _type,
                _subtype,
                messagetype,
                medium,
                invite_identity,
                status,
                from,
                subject,
                body,
                sfootercopy,
                datecreated,
                datemodified
            }.GetHashCode();
        }
    }

    public class From
    {
        public string email { get; set; }
        public string name { get; set; }
    }

    public class Body
    {
        public string text { get; set; }
        public string html { get; set; }
    }
}
