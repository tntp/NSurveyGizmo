using System.Diagnostics;

namespace NSurveyGizmo.Models
{
    public class LocalizableString
    {
        public string English { get; set; }

        public LocalizableString() {}

        public LocalizableString(string str)
        {
            English = str;
        }

        public bool Equals(LocalizableString ls)
        {
            return English == ls.English;
        }
    }
}
