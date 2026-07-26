using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace SCEAMS.MVC.ViewModels;

public sealed class EditAdminUserViewModel
{
    [BindNever]
    public int Id { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập họ và tên.")]
    [StringLength(
        150,
        MinimumLength = 2,
        ErrorMessage = "Họ và tên phải từ 2 đến 150 ký tự.")]
    [RegularExpression(
        ".*\\S.*",
        ErrorMessage = "Họ và tên không được chỉ chứa khoảng trắng.")]
    [Display(Name = "Họ và tên")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập email.")]
    [EmailAddress(ErrorMessage = "Email không đúng định dạng.")]
    [StringLength(256)]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [StringLength(
        50,
        MinimumLength = 4,
        ErrorMessage = "Mã sinh viên phải từ 4 đến 50 ký tự.")]
    [RegularExpression(
        "^[A-Za-z0-9]+$",
        ErrorMessage = "Mã sinh viên chỉ được chứa chữ và số.")]
    [Display(Name = "Mã sinh viên")]
    public string? StudentCode { get; set; }

    [StringLength(
        30,
        ErrorMessage = "Số điện thoại không được vượt quá 30 ký tự.")]
    [RegularExpression(
        "^\\+?[0-9][0-9 .()\\-]{6,28}[0-9]$",
        ErrorMessage = "Số điện thoại không đúng định dạng.")]
    [Display(Name = "Số điện thoại")]
    public string? PhoneNumber { get; set; }

    [BindNever]
    public string Role { get; set; } = string.Empty;

    [BindNever]
    public bool IsActive { get; set; }

    [BindNever]
    public DateTime CreatedAt { get; set; }

    [BindNever]
    public DateTimeOffset CreatedAtLocal { get; set; }

    [BindNever]
    public bool IsNotFound { get; set; }

    [BindNever]
    public string? LoadErrorMessage { get; set; }

    public string RoleLabel => Role switch
    {
        "Admin" => "Quản trị viên",
        "Staff" => "Cán bộ",
        "Organizer" => "Nhà tổ chức",
        "Student" => "Sinh viên",
        _ => Role
    };

    public string RoleCssClass =>
        Role.ToLowerInvariant();
}
