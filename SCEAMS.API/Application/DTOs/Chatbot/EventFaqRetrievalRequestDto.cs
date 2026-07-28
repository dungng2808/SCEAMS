using System.ComponentModel.DataAnnotations;

namespace SCEAMS.Application.DTOs.Chatbot;

public sealed class EventFaqRetrievalRequestDto
{
    [Required(ErrorMessage = "Question là bắt buộc.")]
    [StringLength(500, MinimumLength = 2, ErrorMessage = "Question phải có từ 2 đến 500 ký tự.")]
    public string Question { get; init; } = string.Empty;
}
