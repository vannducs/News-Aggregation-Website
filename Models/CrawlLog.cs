using System.ComponentModel.DataAnnotations;

namespace NewsAggregator.Models
{
    public class CrawlLog
    {
        [Key]
        public int LogID {get;set;}
        public DateTime CrawlTime {get; set;}= DateTime.Now;
        public int ArticleCount {get; set;} =0;
        public string Status {get; set;} = string.Empty;
        public string? ErrorMessage {get; set;}

        public int SourceID {get; set;}

        public Source? Source {get; set;}


    }
}
