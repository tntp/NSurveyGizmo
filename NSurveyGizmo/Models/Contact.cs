// ReSharper disable NonReadonlyMemberInGetHashCode
using System.ComponentModel.DataAnnotations;

namespace NSurveyGizmo.Models
{
    public class Contact
    {
        [Key]
        public int id { get; set; }
        public string emailAddress { get; set; }
        public string firstName { get; set; }
        public string lastName { get; set; }
        public string organization { get; set; }
        public string division { get; set; }
        public string team { get; set; }
        public string group { get; set; }
        public string role { get; set; }
        public string homephone { get; set; }
        public string faxphone { get; set; }
        public string businessphone { get; set; }
        public string mailingaddress { get; set; }
        public string scustomfield5 { get; set; }
        public string scustomfield7 { get; set; }
        public string scustomfield8 { get; set; }
        public string scustomfield9 { get; set; }
        public string scustomfield10 { get; set; }
        public string estatus { get; set; }

        public override bool Equals(object obj)
        {
            return Equals(obj as Contact);
        }

        public bool Equals(Contact contact)
        {
            return id             == contact.id
                && emailAddress   == contact.emailAddress
                && firstName      == contact.firstName
                && lastName       == contact.lastName
                && organization   == contact.organization
                && division       == contact.division
                && team           == contact.team
                && group          == contact.group
                && role           == contact.role
                && homephone      == contact.homephone
                && faxphone       == contact.faxphone
                && businessphone  == contact.businessphone
                && mailingaddress == contact.mailingaddress
                && scustomfield5  == contact.scustomfield5
                && scustomfield7  == contact.scustomfield7
                && scustomfield8  == contact.scustomfield8
                && scustomfield9  == contact.scustomfield9
                && scustomfield10 == contact.scustomfield10
                && estatus        == contact.estatus;
        }

        public override int GetHashCode()
        {
            return new
            {
                id,
                emailAddress,
                firstName,
                lastName,
                organization,
                division,
                team,
                group,
                role,
                homephone,
                faxphone,
                businessphone,
                mailingaddress,
                scustomfield5,
                scustomfield7,
                scustomfield8,
                scustomfield9,
                scustomfield10,
                estatus
            }.GetHashCode();
        }
    }
}
