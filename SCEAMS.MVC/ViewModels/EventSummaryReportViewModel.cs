namespace SCEAMS.MVC.ViewModels;

public sealed class EventSummaryReportViewModel
{
    public DateTime? From { get; init; }
    public DateTime? To { get; init; }
    public int TotalEvents { get; init; }
    public string? ErrorMessage { get; init; }
    public IReadOnlyList<EventSummaryReportItemViewModel> Items { get; init; } = [];
}

public sealed class EventSummaryReportItemViewModel
{
    public string Status { get; init; } = string.Empty;
    public int Count { get; init; }
    public string BadgeClass => Status switch
    {
        "Approved" or "Ongoing" or "Completed" => "status-badge--success",
        "Cancelled" or "Rejected" => "status-badge--danger",
        "PendingApproval" => "status-badge--warning",
        _ => "status-badge--neutral"
    };
}
