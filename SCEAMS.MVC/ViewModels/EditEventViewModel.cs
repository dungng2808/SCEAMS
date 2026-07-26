using System.ComponentModel.DataAnnotations;
using SCEAMS.MVC.Models.Api;

namespace SCEAMS.MVC.ViewModels;

public sealed class EditEventViewModel
{
    public int Id { get; init; }

    [Required(ErrorMessage = "Tiêu đề Event là bắt buộc.")]
    [StringLength(250, MinimumLength = 3, ErrorMessage = "Tiêu đề phải từ 3 đến 250 ký tự.")]
    public string Title { get; init; } = string.Empty;

    [StringLength(4000, ErrorMessage = "Mô tả không được vượt quá 4.000 ký tự.")]
    public string? Description { get; init; }

    public int ClubId { get; init; }
    public string ClubName { get; init; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn Venue.")]
    public int VenueId { get; init; }

    [Required(ErrorMessage = "Vui lòng chọn thời gian bắt đầu.")]
    public DateTime StartTime { get; init; }

    [Required(ErrorMessage = "Vui lòng chọn thời gian kết thúc.")]
    public DateTime EndTime { get; init; }

    [Required(ErrorMessage = "Vui lòng chọn hạn đăng ký.")]
    public DateTime RegistrationDeadline { get; init; }

    [Range(1, int.MaxValue, ErrorMessage = "Capacity phải lớn hơn 0.")]
    public int Capacity { get; init; }

    public string? ErrorMessage { get; set; }
    public IReadOnlyList<VenueApiResponse> Venues { get; set; } = [];
    public bool IsNotFound { get; init; }
    public string? LoadErrorMessage { get; init; }
}
