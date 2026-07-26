using Microsoft.EntityFrameworkCore;
using SCEAMS.Application.Interfaces;
using SCEAMS.Application.Reports;
using SCEAMS.Domain.Enums;
using SCEAMS.Infrastructure.Data;

namespace SCEAMS.Infrastructure.Repositories;

public sealed class ReportRepository : IReportRepository
{
    private readonly SceamsDbContext _context;

    public ReportRepository(SceamsDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<ReportEventSnapshot>> GetEventSnapshotsAsync(
        DateTime? fromUtc,
        DateTime? toUtcExclusive,
        int? organizerId,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Events
            .AsNoTracking()
            .Where(eventEntity =>
                (!fromUtc.HasValue || eventEntity.StartTime >= fromUtc.Value) &&
                (!toUtcExclusive.HasValue || eventEntity.StartTime < toUtcExclusive.Value));

        if (organizerId.HasValue)
        {
            query = query.Where(eventEntity =>
                eventEntity.CreatedByUserId == organizerId.Value ||
                eventEntity.Club.CreatedByUserId == organizerId.Value);
        }

        return await query
            .OrderBy(eventEntity => eventEntity.StartTime)
            .ThenBy(eventEntity => eventEntity.Id)
            .Select(eventEntity => new ReportEventSnapshot(
                eventEntity.Id,
                eventEntity.Title,
                eventEntity.Status,
                eventEntity.StartTime,
                eventEntity.EndTime,
                eventEntity.ClubId,
                eventEntity.Club.Name,
                eventEntity.VenueId,
                eventEntity.Venue.Name,
                eventEntity.Venue.Location,
                eventEntity.Registrations.Count(registration =>
                    registration.Status == RegistrationStatus.Confirmed ||
                    registration.Status == RegistrationStatus.Attended),
                eventEntity.Registrations.Count(registration =>
                    registration.Status == RegistrationStatus.Attended),
                eventEntity.Feedbacks
                    .Select(feedback => (decimal?)feedback.Rating)
                    .Average() ?? 0m))
            .ToListAsync(cancellationToken);
    }
}
