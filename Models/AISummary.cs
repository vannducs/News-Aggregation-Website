using System.ComponentModel.DataAnnotations;

namespace NewsAggregator.Models
{
    public class AISummary
    {
        [Key]
        public int SummaryID {get; set; }
        public int PostID {get; set;}
        public string SummaryText {get; set;} = string.Empty;
        public DateTime CreatedAt {get; set;}= DateTime.Now;
        public string? AIModel {get; set;}

        public Post? Post {get; set;}

    }
}
