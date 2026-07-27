namespace SCEAMS.MVC.Models.Api;

public sealed class AttendanceRateReportApiResponse
{
    public DateTime? From { get; init; }
    public DateTime? To { get; init; }
    public IReadOnlyList<AttendanceRateReportItemApiResponse> Items { get; init; } = [];
}

public sealed class AttendanceRateReportItemApiResponse
{
    public int EventId { get; init; }
    public string EventTitle { get; init; } = string.Empty;
    public string ClubName { get; init; } = string.Empty;
    public DateTime StartTime { get; init; }
    public string Status { get; init; } = string.Empty;
    public int RegisteredCount { get; init; }
    public int AttendedCount { get; init; }
    public decimal AttendanceRate { get; init; }
}
