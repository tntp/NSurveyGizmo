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
    }
}
