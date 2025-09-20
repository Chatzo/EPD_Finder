namespace EPD_Finder.Models
{
    public class ArticleResult
    {
        public string ArticleNumber { get; set; }
        public string ENumber { get; set; }
        public List<string> EpdLinks { get; set; } = new();
        public string Status { get; set; }
    }
}
