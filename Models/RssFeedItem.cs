namespace NewsAggregator.Models
{
    public class RssFeedItem
    {
        public string Title { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string? Summary { get; set; }
        public string? ImageUrl { get; set; }
        public string? Author { get; set; }
        public string? Category { get; set; }
        public DateTime? PublishedDate { get; set; }
    }
}
