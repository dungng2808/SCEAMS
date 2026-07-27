namespace SCEAMS.MVC.ViewModels;

public sealed class AttendanceRateReportViewModel
{
    public DateTime? From { get; init; }
    public DateTime? To { get; init; }
    public string? ErrorMessage { get; init; }
    public IReadOnlyList<AttendanceRateReportItemViewModel> Items { get; init; } = [];
}

public sealed class AttendanceRateReportItemViewModel
{
    public int EventId { get; init; }
    public string EventTitle { get; init; } = string.Empty;
    public string ClubName { get; init; } = string.Empty;
    public DateTime StartTime { get; init; }
    public string Status { get; init; } = string.Empty;
    public int RegisteredCount { get; init; }
    public int AttendedCount { get; init; }
    public decimal AttendanceRate { get; init; }

    public string BadgeClass => Status switch
    {
        "Approved" => "status-badge--approved",
        "Ongoing" => "status-badge--ongoing",
        "Completed" => "status-badge--completed",
        "Cancelled" => "status-badge--cancelled",
        _ => "status-badge--draft"
    };
}
