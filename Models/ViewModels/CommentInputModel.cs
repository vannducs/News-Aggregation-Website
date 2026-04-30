using System.ComponentModel.DataAnnotations;

namespace NewsAggregator.Models.ViewModels
{
    public class CommentInputModel
    {
        public int PostID { get; set; }

        [Required(ErrorMessage = "Vui long nhap ho ten.")]
        [StringLength(100)]
        public string AuthorName { get; set; } = string.Empty;

        [EmailAddress(ErrorMessage = "Email khong hop le.")]
        [StringLength(150)]
        public string? AuthorEmail { get; set; }

        [Required(ErrorMessage = "Vui long nhap noi dung binh luan.")]
        [StringLength(1000)]
        public string Content { get; set; } = string.Empty;
    }
}
