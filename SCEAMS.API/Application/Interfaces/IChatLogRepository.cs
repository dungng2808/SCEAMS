using SCEAMS.Application.Common;
using SCEAMS.Domain.Entities;

namespace SCEAMS.Application.Interfaces;

public interface IChatLogRepository
{
    Task<PagedResult<ChatLog>> GetForStudentAsync(
        int studentId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        ChatLog chatLog,
        CancellationToken cancellationToken = default);

    Task<int> CountSinceAsync(
        int studentId,
        DateTime sinceUtc,
        CancellationToken cancellationToken = default);

    Task<DateTime?> GetOldestSinceAsync(
        int studentId,
        DateTime sinceUtc,
        CancellationToken cancellationToken = default);
}
