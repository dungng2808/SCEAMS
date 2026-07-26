namespace SCEAMS.Application.DTOs.Reports;

public sealed class EventSummaryReportDto
{
    public DateTime? From { get; init; }
    public DateTime? To { get; init; }
    public int TotalEvents { get; init; }
    public IReadOnlyList<EventSummaryItemDto> Items { get; init; } = [];
}

public sealed class EventSummaryItemDto
{
    public string Status { get; init; } = string.Empty;
    public int Count { get; init; }
}
