namespace SCEAMS.Application.DTOs;

public sealed class EventStatusSyncResultDto
{
    public DateTime CheckedAtUtc { get; init; }
    public int ToOngoing { get; init; }
    public int ToCompleted { get; init; }
    public int TotalChanged => ToOngoing + ToCompleted;
}
