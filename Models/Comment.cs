using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NewsAggregator.Models
{
    public class Comment
    {
        [Key]
        public int CommentID { get; set; }

        [Required]
        public int PostID { get; set; }

        public int? UserID { get; set; }

        [NotMapped]
        public string AuthorName { get; set; } = string.Empty;

        [NotMapped]
        public string? AuthorEmail { get; set; }

        [Required]
        [StringLength(1000)]
        public string Content { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Column("Status")]
        public bool IsApproved { get; set; } = true;

        public Post? Post { get; set; }
        public AppUser? User { get; set; }
    }
}