namespace SCEAMS.MVC.Models.Api;

public sealed class EventSummaryReportApiResponse
{
    public DateTime? From { get; init; }
    public DateTime? To { get; init; }
    public int TotalEvents { get; init; }
    public IReadOnlyList<EventSummaryReportItemApiResponse> Items { get; init; } = [];
}

public sealed class EventSummaryReportItemApiResponse
{
    public string Status { get; init; } = string.Empty;
    public int Count { get; init; }
}
