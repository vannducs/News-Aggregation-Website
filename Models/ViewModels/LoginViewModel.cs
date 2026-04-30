using System.ComponentModel.DataAnnotations;

namespace NewsAggregator.Models.ViewModels;

public class LoginViewModel
{
    [Required(ErrorMessage = "Vui long nhap ten dang nhap hoac email.")]
    public string UserNameOrEmail { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui long nhap mat khau.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    public string? ReturnUrl { get; set; }
}
