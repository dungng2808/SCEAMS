using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace SCEAMS.MVC.ViewModels;

public sealed class EditProfileViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập họ và tên.")]
    [StringLength(
        150,
        MinimumLength = 2,
        ErrorMessage = "Họ và tên phải từ 2 đến 150 ký tự.")]
    [RegularExpression(
        ".*\\S.*",
        ErrorMessage = "Họ và tên không được chỉ chứa khoảng trắng.")]
    [Display(Name = "Họ và tên")]
    public string FullName { get; init; } = string.Empty;

    [StringLength(
        30,
        ErrorMessage = "Số điện thoại không được vượt quá 30 ký tự.")]
    [RegularExpression(
        "^\\+?[0-9][0-9 .()\\-]{6,28}[0-9]$",
        ErrorMessage = "Số điện thoại không đúng định dạng.")]
    [Display(Name = "Số điện thoại")]
    public string? PhoneNumber { get; init; }

    [BindNever]
    public string? LoadErrorMessage { get; init; }
}
