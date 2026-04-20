using System.ComponentModel.DataAnnotations;

namespace NewsAggregator.Models.ViewModels;

public class ChangePasswordViewModel
{
    [Required(ErrorMessage = "Vui long nhap mat khau hien tai.")]
    [DataType(DataType.Password)]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui long nhap mat khau moi.")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Mat khau moi phai tu 6 ky tu tro len.")]
    [DataType(DataType.Password)]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui long nhap lai mat khau moi.")]
    [DataType(DataType.Password)]
    [Compare(nameof(NewPassword), ErrorMessage = "Mat khau nhap lai khong khop.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
