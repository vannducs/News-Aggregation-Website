using System.ComponentModel.DataAnnotations;

namespace NewsAggregator.Models.ViewModels;

public class ProfileViewModel
{
    [Required(ErrorMessage = "Vui long nhap ho ten.")]
    [StringLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui long nhap email.")]
    [StringLength(150)]
    [EmailAddress(ErrorMessage = "Email khong hop le.")]
    public string Email { get; set; } = string.Empty;

    [StringLength(20)]
    [Phone(ErrorMessage = "So dien thoai khong hop le.")]
    public string? PhoneNumber { get; set; }

    [DataType(DataType.Date)]
    public DateTime? DateOfBirth { get; set; }

    [StringLength(250)]
    public string? Address { get; set; }

    [StringLength(1000)]
    public string? Bio { get; set; }

    [StringLength(250)]
    public string? AvatarUrl { get; set; }

    public string UserName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
}
