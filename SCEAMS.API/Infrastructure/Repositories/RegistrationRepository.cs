using Microsoft.EntityFrameworkCore;
using SCEAMS.Application.Interfaces;
using SCEAMS.Domain.Entities;
using SCEAMS.Domain.Enums;
using SCEAMS.Infrastructure.Data;

namespace SCEAMS.Infrastructure.Repositories;

public sealed class RegistrationRepository
    : GenericRepository<Registration>, IRegistrationRepository
{
    public RegistrationRepository(SceamsDbContext context)
        : base(context)
    {
    }

    public Task<Registration?> GetByIdWithDetailsAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        return DbSet
            .AsNoTracking()
            .Include(registration => registration.Student)
            .Include(registration => registration.Event)
            .ThenInclude(eventEntity => eventEntity.Club)
            .Include(registration => registration.Attendance)
            .SingleOrDefaultAsync(
                registration => registration.Id == id,
                cancellationToken);
    }

    public Task<Registration?> GetByStudentAndEventAsync(
        int studentId,
        int eventId,
        CancellationToken cancellationToken = default)
    {
        return DbSet.SingleOrDefaultAsync(
            registration =>
                registration.StudentId == studentId &&
                registration.EventId == eventId,
            cancellationToken);
    }

    public Task<int> CountActiveForEventAsync(
        int eventId,
        CancellationToken cancellationToken = default)
    {
        return DbSet.CountAsync(
            registration =>
                registration.EventId == eventId &&
                (registration.Status == RegistrationStatus.Confirmed ||
                    registration.Status == RegistrationStatus.Attended),
            cancellationToken);
    }

    public async Task<(IReadOnlyList<Registration> Items, int TotalItems)> GetForStudentAsync(
        int studentId,
        RegistrationStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet
            .AsNoTracking()
            .Include(registration => registration.Event)
            .Include(registration => registration.Attendance)
            .Where(registration => registration.StudentId == studentId);
        if (status.HasValue)
        {
            query = query.Where(registration => registration.Status == status.Value);
        }

        var totalItems = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(registration => registration.RegisteredAt)
            .Skip((Math.Max(page, 1) - 1) * Math.Clamp(pageSize, 1, 50))
            .Take(Math.Clamp(pageSize, 1, 50))
            .ToListAsync(cancellationToken);
        return (items, totalItems);
    }
}
