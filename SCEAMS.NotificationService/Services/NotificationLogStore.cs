using System.Collections.Concurrent;

namespace SCEAMS.NotificationService.Services;

public sealed record NotificationLogEntry(
    int EventId,
    string EventTitle,
    string EventStatus,
    string NotificationType,
    int RecipientUserId,
    string CorrelationId,
    DateTimeOffset CreatedAt);

public sealed class NotificationLogStore
{
    private readonly ConcurrentDictionary<string, NotificationLogEntry> _entries = new();

    public bool TryAdd(NotificationLogEntry entry) =>
        _entries.TryAdd(entry.CorrelationId, entry);

    public IReadOnlyList<NotificationLogEntry> GetRecent(int limit = 100) =>
        _entries.Values
            .OrderByDescending(entry => entry.CreatedAt)
            .Take(Math.Clamp(limit, 1, 500))
            .ToList();
}
