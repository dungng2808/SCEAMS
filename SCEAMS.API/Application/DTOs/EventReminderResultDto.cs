namespace SCEAMS.Application.DTOs;

public sealed class EventReminderResultDto
{
    public int Scanned { get; init; }
    public int Sent { get; init; }
    public int Skipped { get; init; }
    public int Failed { get; init; }
}
