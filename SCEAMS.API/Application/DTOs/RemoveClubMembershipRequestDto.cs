using System.ComponentModel.DataAnnotations;

namespace SCEAMS.Application.DTOs;

public sealed class RemoveClubMembershipRequestDto
{
    [Required(ErrorMessage = "Lý do loại thành viên là bắt buộc.")]
    [StringLength(1000, MinimumLength = 2, ErrorMessage = "Lý do loại thành viên phải từ 2 đến 1000 ký tự.")]
    [RegularExpression(".*\\S.*", ErrorMessage = "Lý do loại thành viên không được chỉ chứa khoảng trắng.")]
    public string Reason { get; init; } = string.Empty;
}
