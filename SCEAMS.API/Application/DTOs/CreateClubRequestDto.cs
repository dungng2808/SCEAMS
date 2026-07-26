using System.ComponentModel.DataAnnotations;

namespace SCEAMS.Application.DTOs;

public sealed class CreateClubRequestDto
{
    [Required(ErrorMessage = "Tên câu lạc bộ là bắt buộc.")]
    [StringLength(150, MinimumLength = 2, ErrorMessage = "Tên câu lạc bộ phải từ 2 đến 150 ký tự.")]
    [RegularExpression(
        ".*\\S.*",
        ErrorMessage = "Tên câu lạc bộ không được chỉ chứa khoảng trắng.")]
    public string Name { get; init; } = string.Empty;

    [StringLength(1000, ErrorMessage = "Mô tả không được vượt quá 1000 ký tự.")]
    public string? Description { get; init; }

    [Required(ErrorMessage = "Danh mục là bắt buộc.")]
    [Range(1, int.MaxValue, ErrorMessage = "Mã danh mục không hợp lệ.")]
    public int CategoryId { get; init; }
}
