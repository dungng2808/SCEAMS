using System.ComponentModel.DataAnnotations;

namespace SCEAMS.MVC.ViewModels;

public sealed class RejectEventViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập lý do từ chối.")]
    [StringLength(500, MinimumLength = 2, ErrorMessage = "Lý do từ chối phải từ 2 đến 500 ký tự.")]
    [RegularExpression(".*\\S.*", ErrorMessage = "Lý do từ chối không được chỉ chứa khoảng trắng.")]
    public string Reason { get; set; } = string.Empty;
}
