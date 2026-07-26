using System.ComponentModel.DataAnnotations;

namespace SCEAMS.Application.DTOs;

public sealed class RejectEventRequestDto
{
    [Required(ErrorMessage = "Lý do từ chối là bắt buộc.")]
    [StringLength(500, MinimumLength = 2, ErrorMessage = "Lý do từ chối phải từ 2 đến 500 ký tự.")]
    [RegularExpression(".*\\S.*", ErrorMessage = "Lý do từ chối không được chỉ chứa khoảng trắng.")]
    public string Reason { get; init; } = string.Empty;
}
