using System.ComponentModel.DataAnnotations;

namespace NewsAggregator.Models
{
    public class AppUser
    {
        public int AppUserID { get; set; }

        [Required]
        [StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [StringLength(150)]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string UserName { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Password { get; set; } = string.Empty;

        [StringLength(50)]
        public string Role { get; set; } = "Editor";

        [StringLength(20)]
        [Phone]
        public string? PhoneNumber { get; set; }

        public DateTime? DateOfBirth { get; set; }

        [StringLength(250)]
        public string? Address { get; set; }

        [StringLength(1000)]
        public string? Bio { get; set; }

        [StringLength(250)]
        public string? AvatarUrl { get; set; }

        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? LastLoginAt { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }

        public ICollection<Comment> Comments { get; set; } = new List<Comment>();
    }
}
