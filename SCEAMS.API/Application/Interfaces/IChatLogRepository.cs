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
}
