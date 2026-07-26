namespace SCEAMS.Application.DTOs.Reports;

public sealed class AttendanceRateReportDto
{
    public DateTime? From { get; init; }
    public DateTime? To { get; init; }
    public IReadOnlyList<AttendanceRateItemDto> Items { get; init; } = [];
}

public sealed class AttendanceRateItemDto
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
