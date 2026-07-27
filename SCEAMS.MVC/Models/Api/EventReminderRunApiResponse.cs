namespace SCEAMS.MVC.Models.Api;

public sealed class EventReminderRunApiResponse
{
    public int Scanned { get; init; }
    public int Sent { get; init; }
    public int Skipped { get; init; }
    public int Failed { get; init; }
    public string? Message { get; init; }
}
