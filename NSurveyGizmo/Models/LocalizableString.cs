namespace NSurveyGizmo.Models
{
    public class LocalizableString
    {
        public string English { get; set; }

        public bool Equals(LocalizableString ls)
        {
            return English == ls.English;
        }
    }
}
