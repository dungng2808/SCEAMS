using Microsoft.EntityFrameworkCore;
using SCEAMS.Application.Interfaces;
using SCEAMS.Domain.Entities;
using SCEAMS.Domain.Enums;
using SCEAMS.Infrastructure.Data;

namespace SCEAMS.Infrastructure.Repositories;

public sealed class EventRepository
    : GenericRepository<Event>, IEventRepository
{
    public EventRepository(SceamsDbContext context)
        : base(context)
    {
    }

    public Task<Event?> GetByIdWithDetailsAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        return DbSet
            .AsNoTracking()
            .Include(eventEntity => eventEntity.Club)
            .Include(eventEntity => eventEntity.Venue)
            .Include(eventEntity => eventEntity.CreatedByUser)
            .SingleOrDefaultAsync(
                eventEntity => eventEntity.Id == id,
                cancellationToken);
    }

    public Task<bool> HasVenueConflictAsync(
        int venueId,
        DateTime startTime,
        DateTime endTime,
        int? excludedEventId = null,
        CancellationToken cancellationToken = default)
    {
        return DbSet.AnyAsync(
            eventEntity =>
                eventEntity.VenueId == venueId &&
                (!excludedEventId.HasValue ||
                    eventEntity.Id != excludedEventId.Value) &&
                (eventEntity.Status == EventStatus.Approved ||
                    eventEntity.Status == EventStatus.Ongoing) &&
                eventEntity.StartTime < endTime &&
                startTime < eventEntity.EndTime,
            cancellationToken);
    }

    public Task<int> GetConfirmedRegistrationCountAsync(
        int eventId,
        CancellationToken cancellationToken = default)
    {
        return Context.Registrations.CountAsync(
            registration =>
                registration.EventId == eventId &&
                (registration.Status == RegistrationStatus.Confirmed ||
                    registration.Status == RegistrationStatus.Attended),
            cancellationToken);
    }

    public Task<int> GetUpcomingConfirmedRegistrationCountForVenueAsync(
        int venueId,
        DateTime fromUtc,
        CancellationToken cancellationToken = default)
    {
        return Context.Registrations.CountAsync(
            registration =>
                registration.Event.VenueId == venueId &&
                registration.Event.StartTime > fromUtc &&
                registration.Event.Status == EventStatus.Approved &&
                (registration.Status == RegistrationStatus.Confirmed ||
                    registration.Status == RegistrationStatus.Attended),
            cancellationToken);
    }
}
