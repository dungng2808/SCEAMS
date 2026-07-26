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
}
