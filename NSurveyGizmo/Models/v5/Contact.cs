using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NSurveyGizmo.Models.v5
{
    public class Contact
    {
        [Key]
        public int id { get; set; }
        public string customfield5 { get; set; }
        public string customfield6 { get; set; }
        public string customfield7 { get; set; }
        public string customfield8 { get; set; }
        public string customfield9 { get; set; }
        public string customfield10 { get; set; }
        public string status { get; set; }

        
        public string email_address { get; set; }
        public string first_name { get; set; }
        public string last_name { get; set; }
        public string organization { get; set; }
        public string division { get; set; }
        public string department { get; set; }
        public string team { get; set; }
        public string group { get; set; }
        public string role { get; set; }
        public string home_phone { get; set; }
        public string fax_phone { get; set; }
        public string business_phone { get; set; }
        public string mailing_address { get; set; }
        public string mailing_address2 { get; set; }
        public string mailing_address_city { get; set; }
        public string mailing_address_state { get; set; }
        public string mailing_address_country { get; set; }
        public string mailing_address_postal { get; set; }
        public string title { get; set; }
        public string url { get; set; }

        public bool Equals(Contact contact)
        {
            return id == contact.id
                   && email_address == contact.email_address
                   && first_name == contact.first_name
                   && last_name == contact.last_name
                   && organization == contact.organization
                   && division == contact.division
                   && team == contact.team
                   && group == contact.group
                   && role == contact.role
                   && home_phone == contact.home_phone
                   && fax_phone == contact.fax_phone
                   && business_phone == contact.business_phone
                   && mailing_address == contact.mailing_address
                   && customfield5 == contact.customfield5
                   && customfield6 == contact.customfield6
                   && customfield7 == contact.customfield7
                   && customfield8 == contact.customfield8
                   && customfield9 == contact.customfield9
                   && customfield10 == contact.customfield10
                   && status == contact.status;
        }
    }
}
