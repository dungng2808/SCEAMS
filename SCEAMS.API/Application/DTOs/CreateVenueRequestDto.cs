using System.ComponentModel.DataAnnotations;

namespace SCEAMS.Application.DTOs;

public sealed class CreateVenueRequestDto
{
    [Required(ErrorMessage = "Tên địa điểm là bắt buộc.")]
    [StringLength(200, MinimumLength = 2, ErrorMessage = "Tên địa điểm phải từ 2 đến 200 ký tự.")]
    [RegularExpression(".*\\S.*", ErrorMessage = "Tên địa điểm không được chỉ chứa khoảng trắng.")]
    public string Name { get; init; } = string.Empty;

    [Required(ErrorMessage = "Vị trí địa điểm là bắt buộc.")]
    [StringLength(300, MinimumLength = 2, ErrorMessage = "Vị trí phải từ 2 đến 300 ký tự.")]
    [RegularExpression(".*\\S.*", ErrorMessage = "Vị trí không được chỉ chứa khoảng trắng.")]
    public string Location { get; init; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "Sức chứa phải lớn hơn 0.")]
    public int Capacity { get; init; }
}
