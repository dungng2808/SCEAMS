using System.ComponentModel.DataAnnotations;

namespace SCEAMS.Application.DTOs;

public sealed class CreateFeedbackRequestDto
{
    [Range(1, 5, ErrorMessage = "Rating phải từ 1 đến 5.")]
    public int Rating { get; init; }

    [StringLength(2000, ErrorMessage = "Comment không được vượt quá 2000 ký tự.")]
    public string? Comment { get; init; }
}
