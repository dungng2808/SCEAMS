using System.ComponentModel.DataAnnotations;

namespace SCEAMS.Application.DTOs;

public sealed class CancelEventRequestDto
{
    [Required(ErrorMessage = "Lý do hủy là bắt buộc.")]
    [StringLength(500, MinimumLength = 2, ErrorMessage = "Lý do hủy phải từ 2 đến 500 ký tự.")]
    [RegularExpression(".*\\S.*", ErrorMessage = "Lý do hủy không được chỉ chứa khoảng trắng.")]
    public string Reason { get; init; } = string.Empty;
}
