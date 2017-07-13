using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NSurveyGizmo.Models.v5;

namespace NSurveyGizmo.Models.v5
{
    public class LocalizableString
    {
        public string English { get; set; }

        public LocalizableString() { }

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
