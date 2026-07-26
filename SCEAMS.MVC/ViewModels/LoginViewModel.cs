using System.ComponentModel.DataAnnotations;

namespace SCEAMS.MVC.ViewModels;

public sealed class LoginViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập email.")]
    [EmailAddress(ErrorMessage = "Email không đúng định dạng.")]
    [StringLength(256)]
    [Display(Name = "Email")]
    public string Email { get; init; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập mật khẩu.")]
    [StringLength(
        128,
        ErrorMessage = "Mật khẩu không được vượt quá 128 ký tự.")]
    [DataType(DataType.Password)]
    [Display(Name = "Mật khẩu")]
    public string Password { get; init; } = string.Empty;

    public string? ReturnUrl { get; init; }
}
