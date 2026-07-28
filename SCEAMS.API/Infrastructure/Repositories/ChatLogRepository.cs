using Microsoft.EntityFrameworkCore;
using SCEAMS.Application.Common;
using SCEAMS.Application.Interfaces;
using SCEAMS.Domain.Entities;
using SCEAMS.Infrastructure.Data;

namespace SCEAMS.Infrastructure.Repositories;

public sealed class ChatLogRepository : IChatLogRepository
{
    private readonly SceamsDbContext _context;

    public ChatLogRepository(SceamsDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<ChatLog>> GetForStudentAsync(
        int studentId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _context.ChatLogs
            .AsNoTracking()
            .Where(chatLog => chatLog.StudentId == studentId)
            .OrderByDescending(chatLog => chatLog.CreatedAt)
            .ThenByDescending(chatLog => chatLog.Id);
        var totalItems = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        return new PagedResult<ChatLog>(items, totalItems);
    }

    public Task AddAsync(
        ChatLog chatLog,
        CancellationToken cancellationToken = default)
    {
        return _context.ChatLogs.AddAsync(chatLog, cancellationToken).AsTask();
    }
}
