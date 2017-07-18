// ReSharper disable NonReadonlyMemberInGetHashCode
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace NSurveyGizmo.Models
{
    public class Contact
    {
        [Key]
        public int id { get; set; }
        [JsonProperty("customfield5")]
        public string scustomfield5 { get; set; }
        [JsonProperty("customfield6")]
        public string scustomfield6 { get; set; }
        [JsonProperty("customfield7")]
        public string scustomfield7 { get; set; }
        [JsonProperty("customfield8")]
        public string scustomfield8 { get; set; }
        [JsonProperty("customfield9")]
        public string scustomfield9 { get; set; }
        [JsonProperty("customfield10")]
        public string scustomfield10 { get; set; }
        [JsonProperty("status")]
        public string estatus { get; set; }
        [JsonProperty("email_address")]
        public string semailaddress { get; set; }
        [JsonProperty("first_name")]
        public string sfirstname { get; set; }
        [JsonProperty("last_name")]
        public string slastname { get; set; }
        [JsonProperty("organization")]
        public string sorganization { get; set; }
        [JsonProperty("division")]
        public string sdivision { get; set; }
        [JsonProperty("department")]
        public string sdepartment { get; set; }
        [JsonProperty("team")]
        public string steam { get; set; }
        [JsonProperty("group")]
        public string sgroup { get; set; }
        [JsonProperty("role")]
        public string srole { get; set; }
        [JsonProperty("home_phone")]
        public string shomephone { get; set; }
        [JsonProperty("fax_phone")]
        public string sfaxphone { get; set; }
        [JsonProperty("business_phone")]
        public string sbusinessphone { get; set; }
        [JsonProperty("mailing_address")]
        public string smailingaddress { get; set; }
        [JsonProperty("mailing_address2")]
        public string smailingaddress2 { get; set; }
        [JsonProperty("mailing_address_city")]
        public string smailingaddresscity { get; set; }
        [JsonProperty("mailing_address_state")]
        public string smailingaddressstate { get; set; }

        public bool Equals(Contact contact)
        {
            return id              == contact.id
                && semailaddress   == contact.semailaddress
                && sfirstname      == contact.sfirstname
                && slastname       == contact.slastname
                && sorganization   == contact.sorganization
                && sdivision       == contact.sdivision
                && steam           == contact.steam
                && sgroup          == contact.sgroup
                && srole           == contact.srole
                && shomephone      == contact.shomephone
                && sfaxphone       == contact.sfaxphone
                && sbusinessphone  == contact.sbusinessphone
                && smailingaddress == contact.smailingaddress
                && scustomfield5   == contact.scustomfield5
                && scustomfield7   == contact.scustomfield7
                && scustomfield8   == contact.scustomfield8
                && scustomfield9   == contact.scustomfield9
                && scustomfield10  == contact.scustomfield10
                && estatus         == contact.estatus;
        }
    }
}
