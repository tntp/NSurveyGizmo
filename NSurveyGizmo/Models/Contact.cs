// ReSharper disable NonReadonlyMemberInGetHashCode
using System.ComponentModel.DataAnnotations;

namespace NSurveyGizmo.Models
{
    public class Contact
    {
        [Key]
        public int id { get; set; }
        public string scustomfield5 { get; set; }
        public string scustomfield7 { get; set; }
        public string scustomfield8 { get; set; }
        public string scustomfield9 { get; set; }
        public string scustomfield10 { get; set; }
        public string estatus { get; set; }

        // TODO: Update Equals() method to account for new properties
        public string semailaddress { get; set; }
        public string sfirstname { get; set; }
        public string slastname { get; set; }
        public string sorganization { get; set; }
        public string sdivision { get; set; }
        public string sdepartment { get; set; }
        public string steam { get; set; }
        public string sgroup { get; set; }
        public string srole { get; set; }
        public string shomephone { get; set; }
        public string sfaxphone { get; set; }
        public string sbusinessphone { get; set; }
        public string smailingaddress { get; set; }
        public string smailingaddress2 { get; set; }
        public string smailingaddresscity { get; set; }
        public string smailingaddressstate { get; set; }

        public bool Equals(Contact contact)
        {
            return id             == contact.id
                && semailaddress  == contact.semailaddress
                && sfirstname     == contact.sfirstname
                && slastname      == contact.slastname
                && sorganization  == contact.sorganization
                && sdivision      == contact.sdivision
                && steam          == contact.steam
                && sgroup         == contact.sgroup
                && srole          == contact.srole
                && shomephone     == contact.shomephone
                && sfaxphone      == contact.sfaxphone
                && sbusinessphone == contact.sbusinessphone
                && smailingaddress== contact.smailingaddress
                && scustomfield5  == contact.scustomfield5
                && scustomfield7  == contact.scustomfield7
                && scustomfield8  == contact.scustomfield8
                && scustomfield9  == contact.scustomfield9
                && scustomfield10 == contact.scustomfield10
                && estatus        == contact.estatus;
        }
    }
}
