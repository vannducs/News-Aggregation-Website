using System.ComponentModel.DataAnnotations;

namespace NewsAggregator.Models.ViewModels;

public class RegisterViewModel
{
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

    [StringLength(20)]
    [Phone(ErrorMessage = "So dien thoai khong hop le.")]
    public string? PhoneNumber { get; set; }

    [Required(ErrorMessage = "Vui long nhap mat khau.")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Mat khau phai tu 6 ky tu tro len.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui long nhap lai mat khau.")]
    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "Mat khau nhap lai khong khop.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
