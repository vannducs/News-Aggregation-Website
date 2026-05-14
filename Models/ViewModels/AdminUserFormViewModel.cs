using System.ComponentModel.DataAnnotations;

namespace NewsAggregator.Models.ViewModels;

public class AdminUserFormViewModel
{
    public static readonly string[] AvailableRoles =
    [
        UserRoles.Admin,
        UserRoles.Editor,
        UserRoles.Moderator,
        UserRoles.Member
    ];

    public int? AppUserID { get; set; }

    [Required(ErrorMessage = "Vui long nhap ho ten.")]
    [StringLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui long nhap email.")]
    [StringLength(150)]
    [EmailAddress(ErrorMessage = "Email khong hop le.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui long nhap ten dang nhap.")]
    [StringLength(100)]
    public string UserName { get; set; } = string.Empty;

    [StringLength(100, MinimumLength = 6, ErrorMessage = "Mat khau phai tu 6 ky tu tro len.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui long chon vai tro.")]
    [StringLength(50)]
    public string Role { get; set; } = UserRoles.Editor;

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

    public bool IsActive { get; set; } = true;

    public bool IsEdit => AppUserID.HasValue;
}
