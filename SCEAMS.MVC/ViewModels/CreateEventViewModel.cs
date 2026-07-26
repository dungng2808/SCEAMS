using System.ComponentModel.DataAnnotations;
using SCEAMS.MVC.Models.Api;

namespace SCEAMS.MVC.ViewModels;

public sealed class CreateEventViewModel
{
    [Required(ErrorMessage = "Tiêu đề Event là bắt buộc.")]
    [StringLength(250, MinimumLength = 3, ErrorMessage = "Tiêu đề phải từ 3 đến 250 ký tự.")]
    public string Title { get; set; } = string.Empty;

    [StringLength(4000, ErrorMessage = "Mô tả không được vượt quá 4.000 ký tự.")]
    public string? Description { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn Club.")]
    [Display(Name = "Club")]
    public int ClubId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn Venue.")]
    [Display(Name = "Địa điểm")]
    public int VenueId { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn thời gian bắt đầu.")]
    [Display(Name = "Bắt đầu")]
    public DateTime StartTime { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn thời gian kết thúc.")]
    [Display(Name = "Kết thúc")]
    public DateTime EndTime { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn hạn đăng ký.")]
    [Display(Name = "Hạn đăng ký")]
    public DateTime RegistrationDeadline { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Capacity phải lớn hơn 0.")]
    public int Capacity { get; set; }

    public string? ErrorMessage { get; set; }
    public IReadOnlyList<ClubApiResponse> Clubs { get; set; } = [];
    public IReadOnlyList<VenueApiResponse> Venues { get; set; } = [];
}
