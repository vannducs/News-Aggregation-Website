using System.ComponentModel.DataAnnotations;

namespace NewsAggregator.Models
{
    public class Source
    {
        [Key]
        public int SourceID {get; set;}
        public string SourceName {get; set;} = string.Empty;
        public string RssUrl {get; set;} = string.Empty;
        public string? WebsiteUrl {get; set;}
        public string? LogoUrl {get; set;}
        public bool IsActive {get; set;} = true;
        public ICollection<Post> Posts {get; set;} = new List<Post>();
        public ICollection<CrawlLog> CrawlLogs {get; set;} = new List<CrawlLog>();
    }
}