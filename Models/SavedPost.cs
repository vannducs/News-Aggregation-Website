using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NewsAggregator.Models
{
    [Table("tblSavedPosts")]
    public class SavedPost
    {
        [Key]
        public int SavedPostID { get; set; }
        public int UserID { get; set; }
        public int PostID { get; set; }
        public DateTime SavedAt { get; set; } = DateTime.Now;

        public AppUser? User { get; set; }
        public Post? Post { get; set; }
    }
}
