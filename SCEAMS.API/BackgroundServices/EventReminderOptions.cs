namespace SCEAMS.Api.BackgroundServices;

public sealed class EventReminderOptions
{
    public const string SectionName = "EventReminder";

    public bool Enabled { get; set; } = true;
    public int IntervalSeconds { get; set; } = 300;
}
