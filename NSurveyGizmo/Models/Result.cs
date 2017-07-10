namespace NSurveyGizmo.Models
{
    public class Result
    {
        public int id { get; set; }
        public bool result_ok { get; set; }
    }

    public class Result<T>
    {
        public bool result_ok { get; set; }
        public T Data { get; set; }
    }

    public class PagedResult<T>
    {
        public bool result_ok { get; set; }
        public string total_count { get; set; }
        public int page { get; set; }
        public int total_pages { get; set; }
        public int results_per_page { get; set; }
        public T[] Data { get; set; }
    }
}
