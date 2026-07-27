using SCEAMS.MVC.Models.Api;

namespace SCEAMS.MVC.Models;

public sealed class EventReminderRunApiResult
{
    public bool IsSuccess { get; init; }
    public bool IsUnauthorized { get; init; }
    public bool IsForbidden { get; init; }
    public string? ErrorMessage { get; init; }
    public EventReminderRunApiResponse? Summary { get; init; }
}
