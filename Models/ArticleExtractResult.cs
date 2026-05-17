namespace NewsAggregator.Models
{
    public class ArticleExtractResult
    {
        public string? Title { get; set; }
        public string? Content { get; set; }
        public string? ContentText { get; set; }
        public string? ImageUrl { get; set; }
        public string? Author { get; set; }
        public DateTime? PublishedDate { get; set; }
        public string? SourceUrl { get; set; }
        public bool IsSuccess { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
