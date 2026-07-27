using SCEAMS.MVC.Models.Api;

namespace SCEAMS.MVC.Models;

public sealed class NotificationLogApiResult
{
    public bool IsSuccess { get; init; }
    public bool IsUnauthorized { get; init; }
    public bool IsForbidden { get; init; }
    public string? ErrorMessage { get; init; }
    public IReadOnlyList<NotificationLogApiResponse> Entries { get; init; } = [];
}
