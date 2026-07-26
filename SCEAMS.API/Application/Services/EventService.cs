using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using SCEAMS.Application.DTOs;
using SCEAMS.Application.Interfaces;
using SCEAMS.Application.Common;
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

    public async Task<Result<EventDetailResponseDto>> GetEventByIdAsync(
        int id,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default)
    {
        var eventEntity = await _unitOfWork.Events.GetByIdWithDetailsAsync(
            id,
            cancellationToken);
        if (eventEntity == null)
        {
            return Result<EventDetailResponseDto>.Fail(
                $"Event với ID {id} không tồn tại.",
                StatusCodes.Status404NotFound);
        }

        var isAdminOrStaff = user.IsInRole(nameof(UserRole.Admin)) ||
                             user.IsInRole(nameof(UserRole.Staff));
        var currentUserId = GetUserId(user);
        var isOwner = currentUserId.HasValue &&
                      (eventEntity.CreatedByUserId == currentUserId.Value ||
                       eventEntity.Club.CreatedByUserId == currentUserId.Value);

        if (!isAdminOrStaff && !isOwner && eventEntity.Status != EventStatus.Approved)
        {
            return Result<EventDetailResponseDto>.Fail(
                $"Event với ID {id} không tồn tại.",
                StatusCodes.Status404NotFound);
        }

        var registeredCount = await _unitOfWork.Events
            .GetConfirmedRegistrationCountAsync(id, cancellationToken);
        var slotsRemaining = Math.Max(0, eventEntity.Capacity - registeredCount);
        var isOrganizer = user.IsInRole(nameof(UserRole.Organizer));
        var isStudent = user.IsInRole(nameof(UserRole.Student));
        var now = DateTime.UtcNow;
        var canManage = isAdminOrStaff || (isOrganizer && isOwner);

        return Result<EventDetailResponseDto>.Ok(new EventDetailResponseDto
        {
            Id = eventEntity.Id,
            Title = eventEntity.Title,
            Description = eventEntity.Description,
            Status = eventEntity.Status,
            ClubId = eventEntity.ClubId,
            ClubName = eventEntity.Club.Name,
            VenueId = eventEntity.VenueId,
            VenueName = eventEntity.Venue.Name,
            VenueLocation = eventEntity.Venue.Location,
            StartTime = eventEntity.StartTime,
            EndTime = eventEntity.EndTime,
            RegistrationDeadline = eventEntity.RegistrationDeadline,
            Capacity = eventEntity.Capacity,
            RegisteredCount = registeredCount,
            SlotsRemaining = slotsRemaining,
            CreatedByUserId = eventEntity.CreatedByUserId,
            CreatedByUserName = eventEntity.CreatedByUser.FullName,
            RejectionReason = eventEntity.RejectionReason,
            CancellationReason = eventEntity.CancellationReason,
            Permissions = new EventActionPermissionsDto
            {
                CanEdit = canManage && eventEntity.Status is
                    EventStatus.Draft or EventStatus.PendingApproval,
                CanSubmit = isOrganizer && isOwner &&
                            eventEntity.Status == EventStatus.Draft,
                CanApprove = isAdminOrStaff &&
                             eventEntity.Status == EventStatus.PendingApproval,
                CanReject = isAdminOrStaff &&
                            eventEntity.Status == EventStatus.PendingApproval,
                CanCancel = (isAdminOrStaff || isOwner) &&
                            eventEntity.Status is not
                                (EventStatus.Completed or EventStatus.Cancelled) &&
                            (isAdminOrStaff || eventEntity.StartTime > now),
                CanRegister = isStudent &&
                              eventEntity.Status == EventStatus.Approved &&
                              eventEntity.RegistrationDeadline > now &&
                              slotsRemaining > 0
            }
        });
    }

    private static int? GetUserId(ClaimsPrincipal user)
    {
        var value = user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? user.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return int.TryParse(value, out var id) && id > 0 ? id : null;
    }
}
