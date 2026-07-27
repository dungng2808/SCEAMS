using SCEAMS.Domain.Entities;

namespace SCEAMS.Application.Interfaces;

public interface IEventRepository : IGenericRepository<Event>
{
    IQueryable<Event> GetQueryable();

    Task<Event?> GetByIdWithDetailsAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<bool> HasVenueConflictAsync(
        int venueId,
        DateTime startTime,
        DateTime endTime,
        int? excludedEventId = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Event>> GetVenueConflictsAsync(
        int venueId,
        DateTime startTime,
        DateTime endTime,
        int? excludedEventId = null,
        CancellationToken cancellationToken = default);

    Task<int> GetConfirmedRegistrationCountAsync(
        int eventId,
        CancellationToken cancellationToken = default);

    Task<int> GetUpcomingConfirmedRegistrationCountForVenueAsync(
        int venueId,
        DateTime fromUtc,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Event>> GetActiveEventsForVenueAsync(
        int venueId,
        DateTime fromUtc,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Event>> GetVenueScheduleAsync(
        int venueId,
        DateTime fromUtc,
        DateTime toUtc,
        bool includeInternalStatuses,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Event>> GetEventsWithUpcomingDeadlineAsync(
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken = default);
}
