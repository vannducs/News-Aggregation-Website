namespace NewsAggregation.Models
{
    public class Post
    {
        public int PostID {get; set;}
        public string Title {get; set;} =string.Empty;
        public string? Abstract {get; set;}
        public string? Contents {get; set;}
        public string? Images {get; set;}
        public string? Link {get; set;}
        public string? Author {get; set;}
        public DateTime CreatedDate {get; set;}= DateTime.Now;
        public bool IsActive {get; set;} = true;
        public int PostOrder {get; set;} = 0;
        public DateTime? CrawledAt {get; set;}
        public int ViewCount {get; set;} =0;

        public int MenuID {get; set;}
        public int? SourceID {get; set;}

        public Menu? Menu {get; set;}
        public Source? Source {get; set;}
        public AISummary? AISummary {get; set;}
    }
}