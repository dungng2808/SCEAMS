using System.ComponentModel.DataAnnotations;

namespace SCEAMS.MVC.ViewModels;

public sealed class SubmitFeedbackViewModel
{
    [Range(1, 5, ErrorMessage = "Rating phải từ 1 đến 5.")]
    public int Rating { get; set; }

    [StringLength(2000, ErrorMessage = "Comment không được vượt quá 2000 ký tự.")]
    public string? Comment { get; set; }
}
