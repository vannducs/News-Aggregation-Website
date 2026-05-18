namespace NewsAggregator.Models.ViewModels
{
    public class SourceListItem
    {
        public int SourceID { get; set; }
        public string SourceName { get; set; } = string.Empty;
        public string RssUrl { get; set; } = string.Empty;
        public string? WebsiteUrl { get; set; }
        public string? LogoUrl { get; set; }
        public bool IsActive { get; set; }
        public int PostCount { get; set; }
        public DateTime? LastCrawledAt { get; set; }
        public string? LastCrawlStatus { get; set; }
    }
}
