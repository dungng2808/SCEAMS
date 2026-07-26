using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using SCEAMS.Application.DTOs;
using SCEAMS.Application.Interfaces;
using SCEAMS.Domain.Enums;

namespace SCEAMS.Application.Services;

public sealed class EventService : IEventService
{
    private readonly IUnitOfWork _unitOfWork;

    public EventService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public IQueryable<EventListResponseDto> GetEventsQuery(ClaimsPrincipal user)
    {
        var query = _unitOfWork.Events.GetQueryable();
        var isAdminOrStaff = user.IsInRole(nameof(UserRole.Admin)) ||
                             user.IsInRole(nameof(UserRole.Staff));
        var isOrganizer = user.IsInRole(nameof(UserRole.Organizer));

        if (!isAdminOrStaff && !isOrganizer)
        {
            query = query.Where(eventEntity =>
                eventEntity.Status == EventStatus.Approved);
        }
        else if (isOrganizer && !isAdminOrStaff)
        {
            var userId = GetUserId(user);
            query = userId.HasValue
                ? query.Where(eventEntity =>
                    eventEntity.CreatedByUserId == userId.Value ||
                    eventEntity.Club.CreatedByUserId == userId.Value)
                : query.Where(_ => false);
        }

        return query.Select(eventEntity => new EventListResponseDto
        {
            Id = eventEntity.Id,
            Title = eventEntity.Title,
            Status = eventEntity.Status,
            ClubId = eventEntity.ClubId,
            ClubName = eventEntity.Club.Name,
            Club = new EventClubSummaryDto
            {
                Id = eventEntity.Club.Id,
                Name = eventEntity.Club.Name
            },
            VenueId = eventEntity.VenueId,
            VenueName = eventEntity.Venue.Name,
            Venue = new EventVenueSummaryDto
            {
                Id = eventEntity.Venue.Id,
                Name = eventEntity.Venue.Name,
                Location = eventEntity.Venue.Location
            },
            StartTime = eventEntity.StartTime,
            EndTime = eventEntity.EndTime,
            RegistrationDeadline = eventEntity.RegistrationDeadline,
            Capacity = eventEntity.Capacity,
            RegisteredCount = eventEntity.Registrations.Count(registration =>
                registration.Status == RegistrationStatus.Confirmed ||
                registration.Status == RegistrationStatus.Attended),
            SlotsRemaining = Math.Max(
                0,
                eventEntity.Capacity - eventEntity.Registrations.Count(registration =>
                    registration.Status == RegistrationStatus.Confirmed ||
                    registration.Status == RegistrationStatus.Attended)),
            CreatedByUserId = eventEntity.CreatedByUserId,
            CreatedByUserName = eventEntity.CreatedByUser.FullName
        });
    }

    private static int? GetUserId(ClaimsPrincipal user)
    {
        var value = user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? user.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return int.TryParse(value, out var id) && id > 0 ? id : null;
    }
}
