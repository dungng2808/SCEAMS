using System.ComponentModel.DataAnnotations;

namespace SCEAMS.MVC.ViewModels;

public sealed class CreateAdminUserViewModel : IValidatableObject
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

    [Required(ErrorMessage = "Vui lòng nhập email.")]
    [EmailAddress(ErrorMessage = "Email không đúng định dạng.")]
    [StringLength(256)]
    [Display(Name = "Email")]
    public string Email { get; init; } = string.Empty;

    [StringLength(
        50,
        MinimumLength = 4,
        ErrorMessage = "Mã sinh viên phải từ 4 đến 50 ký tự.")]
    [RegularExpression(
        "^[A-Za-z0-9]+$",
        ErrorMessage = "Mã sinh viên chỉ được chứa chữ và số.")]
    [Display(Name = "Mã sinh viên")]
    public string? StudentCode { get; init; }

    [StringLength(
        30,
        ErrorMessage = "Số điện thoại không được vượt quá 30 ký tự.")]
    [RegularExpression(
        "^\\+?[0-9][0-9 .()\\-]{6,28}[0-9]$",
        ErrorMessage = "Số điện thoại không đúng định dạng.")]
    [Display(Name = "Số điện thoại")]
    public string? PhoneNumber { get; init; }

    [Required(ErrorMessage = "Vui lòng chọn vai trò.")]
    [RegularExpression(
        "^(Admin|Staff|Organizer|Student)$",
        ErrorMessage = "Vai trò không hợp lệ.")]
    [Display(Name = "Vai trò")]
    public string Role { get; init; } = "Student";

    [Display(Name = "Kích hoạt tài khoản")]
    public bool IsActive { get; init; } = true;

    [Required(ErrorMessage = "Vui lòng nhập mật khẩu.")]
    [StringLength(
        128,
        MinimumLength = 8,
        ErrorMessage = "Mật khẩu phải từ 8 đến 128 ký tự.")]
    [RegularExpression(
        "^(?=.*[a-z])(?=.*[A-Z])(?=.*\\d)(?=.*[^A-Za-z0-9\\s]).+$",
        ErrorMessage = "Mật khẩu phải có chữ hoa, chữ thường, số và ký tự đặc biệt.")]
    [DataType(DataType.Password)]
    [Display(Name = "Mật khẩu")]
    public string Password { get; init; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng xác nhận mật khẩu.")]
    [Compare(
        nameof(Password),
        ErrorMessage = "Mật khẩu xác nhận không khớp.")]
    [DataType(DataType.Password)]
    [Display(Name = "Xác nhận mật khẩu")]
    public string ConfirmPassword { get; init; } = string.Empty;

    public IEnumerable<ValidationResult> Validate(
        ValidationContext validationContext)
    {
        if (string.Equals(
                Role,
                "Student",
                StringComparison.Ordinal) &&
            string.IsNullOrWhiteSpace(StudentCode))
        {
            yield return new ValidationResult(
                "Vui lòng nhập mã sinh viên cho tài khoản sinh viên.",
                [nameof(StudentCode)]);
        }
    }
}
