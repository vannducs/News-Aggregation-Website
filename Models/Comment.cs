using System.ComponentModel.DataAnnotations;

namespace NewsAggregator.Models
{
    public class Comment
    {
        public int CommentID { get; set; }

        [Required]
        public int PostID { get; set; }

        public int? UserID { get; set; }

        [Required]
        [StringLength(100)]
        public string AuthorName { get; set; } = string.Empty;

        [StringLength(150)]
        [EmailAddress]
        public string? AuthorEmail { get; set; }

        [Required]
        [StringLength(1000)]
        public string Content { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public bool IsApproved { get; set; } = true;

        public Post? Post { get; set; }
        public AppUser? User { get; set; }
    }
}
