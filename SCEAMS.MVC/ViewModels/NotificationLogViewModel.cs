using SCEAMS.MVC.Models.Api;

namespace SCEAMS.MVC.ViewModels;

public sealed class NotificationLogViewModel
{
    public int? EventId { get; init; }
    public string? NotificationType { get; init; }
    public bool? Success { get; init; }
    public string? ErrorMessage { get; init; }
    public IReadOnlyList<NotificationLogApiResponse> Entries { get; init; } = [];
}
