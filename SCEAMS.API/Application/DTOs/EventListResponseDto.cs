using SCEAMS.Domain.Enums;

namespace SCEAMS.Application.DTOs;

public sealed class EventListResponseDto
{
    public int Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public EventStatus Status { get; init; }
    public int ClubId { get; init; }
    public string ClubName { get; init; } = string.Empty;
    public EventClubSummaryDto Club { get; init; } = new();
    public int VenueId { get; init; }
    public string VenueName { get; init; } = string.Empty;
    public EventVenueSummaryDto Venue { get; init; } = new();
    public DateTime StartTime { get; init; }
    public DateTime EndTime { get; init; }
    public DateTime RegistrationDeadline { get; init; }
    public int Capacity { get; init; }
    public int RegisteredCount { get; init; }
    public int SlotsRemaining { get; init; }
    public int CreatedByUserId { get; init; }
    public string CreatedByUserName { get; init; } = string.Empty;
}

public sealed class EventClubSummaryDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
}

public sealed class EventVenueSummaryDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Location { get; init; } = string.Empty;
}
