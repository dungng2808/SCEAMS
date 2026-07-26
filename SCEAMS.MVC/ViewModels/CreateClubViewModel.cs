using System.ComponentModel.DataAnnotations;

namespace SCEAMS.MVC.ViewModels;

public sealed class CreateClubViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập tên câu lạc bộ.")]
    [StringLength(150, MinimumLength = 2, ErrorMessage = "Tên câu lạc bộ phải từ 2 đến 150 ký tự.")]
    [Display(Name = "Tên câu lạc bộ")]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000, ErrorMessage = "Mô tả không được vượt quá 1000 ký tự.")]
    [Display(Name = "Mô tả câu lạc bộ")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn danh mục.")]
    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn danh mục hợp lệ.")]
    [Display(Name = "Danh mục / Lĩnh vực")]
    public int CategoryId { get; set; }

    public IReadOnlyList<ClubCategorySelectItemViewModel> Categories { get; set; } = [];

    public string? ErrorMessage { get; set; }
}
