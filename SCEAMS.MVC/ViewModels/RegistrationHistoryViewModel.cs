namespace SCEAMS.MVC.ViewModels;

public sealed class RegistrationHistoryViewModel
{
    public string? Status { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 10;
    public int TotalItems { get; init; }
    public int TotalPages { get; init; }
    public bool HasPreviousPage { get; init; }
    public bool HasNextPage { get; init; }
    public string? ErrorMessage { get; init; }
    public IReadOnlyList<RegistrationHistoryItemViewModel> Items { get; init; } = [];
}

public sealed class RegistrationHistoryItemViewModel
{
    public int Id { get; init; }
    public int EventId { get; init; }
    public string EventTitle { get; init; } = string.Empty;
    public string EventStatus { get; init; } = string.Empty;
    public DateTime StartTime { get; init; }
    public DateTime EndTime { get; init; }
    public string RegistrationStatus { get; init; } = string.Empty;
    public DateTime RegisteredAt { get; init; }
    public DateTime? CancelledAt { get; init; }
    public bool IsAttended { get; init; }
    public DateTime? CheckInTime { get; init; }
    public bool CanCancel => RegistrationStatus == "Confirmed" &&
                             StartTime > DateTime.UtcNow.AddHours(24);
}
