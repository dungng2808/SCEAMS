using System.Collections.Concurrent;
using SCEAMS.Application.DTOs;
using SCEAMS.Application.Interfaces;

namespace SCEAMS.Infrastructure.GrpcClients;

public sealed class NotificationLogStore : INotificationLogStore
{
    private readonly ConcurrentQueue<NotificationLogEntryDto> _entries = new();

    public void Add(NotificationLogEntryDto entry)
    {
        _entries.Enqueue(entry);
        while (_entries.Count > 500)
        {
            _entries.TryDequeue(out _);
        }
    }

    public IReadOnlyList<NotificationLogEntryDto> GetRecent(int limit = 100) =>
        _entries
            .Reverse()
            .Take(Math.Clamp(limit, 1, 500))
            .ToList();
}
