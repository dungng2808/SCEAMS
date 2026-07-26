using System.ComponentModel.DataAnnotations;

namespace SCEAMS.MVC.ViewModels;

public sealed class RejectClubViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập lý do từ chối.")]
    [StringLength(500, MinimumLength = 2, ErrorMessage = "Lý do từ chối phải từ 2 đến 500 ký tự.")]
    [RegularExpression(".*\\S.*", ErrorMessage = "Lý do từ chối không được chỉ chứa khoảng trắng.")]
    [Display(Name = "Lý do từ chối")]
    public string Reason { get; set; } = string.Empty;
}
