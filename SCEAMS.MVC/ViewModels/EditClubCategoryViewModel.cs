using System.ComponentModel.DataAnnotations;

namespace SCEAMS.MVC.ViewModels;

public sealed class EditClubCategoryViewModel
{
    public int Id { get; init; }

    [Required(ErrorMessage = "Vui lòng nhập tên danh mục.")]
    [StringLength(
        150,
        ErrorMessage = "Tên danh mục không được vượt quá 150 ký tự.")]
    [RegularExpression(
        ".*\\S.*",
        ErrorMessage = "Tên danh mục không được chỉ chứa khoảng trắng.")]
    [Display(Name = "Tên danh mục")]
    public string Name { get; init; } = string.Empty;

    [StringLength(
        1000,
        ErrorMessage = "Mô tả không được vượt quá 1.000 ký tự.")]
    [Display(Name = "Mô tả")]
    public string? Description { get; init; }

    public bool IsNotFound { get; init; }
    public string? LoadErrorMessage { get; init; }
}
