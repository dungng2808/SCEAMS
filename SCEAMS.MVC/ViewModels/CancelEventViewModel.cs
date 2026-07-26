using System.ComponentModel.DataAnnotations;

namespace SCEAMS.MVC.ViewModels;

public sealed class CancelEventViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập lý do hủy Event.")]
    [StringLength(500, MinimumLength = 2, ErrorMessage = "Lý do hủy phải từ 2 đến 500 ký tự.")]
    [RegularExpression(".*\\S.*", ErrorMessage = "Lý do hủy không được chỉ chứa khoảng trắng.")]
    public string Reason { get; set; } = string.Empty;
}
