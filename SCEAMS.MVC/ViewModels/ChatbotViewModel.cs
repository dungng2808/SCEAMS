using System.ComponentModel.DataAnnotations;

namespace SCEAMS.MVC.ViewModels;

public sealed class ChatbotViewModel
{
    [Required(ErrorMessage = "Hãy nhập câu hỏi của bạn.")]
    [StringLength(500, MinimumLength = 2, ErrorMessage = "Câu hỏi phải có từ 2 đến 500 ký tự.")]
    public string Question { get; set; } = string.Empty;
    public IReadOnlyList<ChatbotEventViewModel> RelatedEvents { get; init; } = [];
    public bool HasSearched { get; init; }
    public string? Answer { get; init; }
    public string? ErrorMessage { get; init; }
}

public sealed class ChatbotEventViewModel
{
    public int Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string ClubName { get; init; } = string.Empty;
    public string VenueName { get; init; } = string.Empty;
    public DateTime StartTime { get; init; }
    public DateTime EndTime { get; init; }
    public int Capacity { get; init; }
    public int RegisteredCount { get; init; }
    public int SlotsRemaining { get; init; }
}

public sealed class ChatHistoryViewModel
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 10;
    public int TotalItems { get; init; }
    public int TotalPages { get; init; }
    public bool HasPreviousPage { get; init; }
    public bool HasNextPage { get; init; }
    public string? ErrorMessage { get; init; }
    public IReadOnlyList<ChatHistoryItemViewModel> Items { get; init; } = [];
}

public sealed class ChatHistoryItemViewModel
{
    public int Id { get; init; }
    public string Question { get; init; } = string.Empty;
    public string AnswerText { get; init; } = string.Empty;
    public IReadOnlyList<int> RelatedEventIds { get; init; } = [];
    public DateTime CreatedAt { get; init; }
}
