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

    public async Task<Result<EventDetailResponseDto>> CreateEventAsync(
        CreateEventRequestDto request,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default)
    {
        var organizerId = GetUserId(user);
        if (!organizerId.HasValue)
        {
            return Result<EventDetailResponseDto>.Fail(
                "Không xác định được Organizer từ token.",
                StatusCodes.Status401Unauthorized);
        }

        var title = request.Title.Trim();
        var club = await _unitOfWork.Clubs.GetByIdWithDetailsAsync(
            request.ClubId,
            cancellationToken);
        if (club == null)
        {
            return Result<EventDetailResponseDto>.Fail(
                "Club không tồn tại.",
                StatusCodes.Status404NotFound);
        }

        if (club.CreatedByUserId != organizerId.Value)
        {
            return Result<EventDetailResponseDto>.Fail(
                "Organizer chỉ được tạo Event cho Club mình phụ trách.",
                StatusCodes.Status403Forbidden);
        }

        if (club.Status != ClubStatus.Approved)
        {
            return Result<EventDetailResponseDto>.Fail(
                "Club phải ở trạng thái Approved trước khi tạo Event.",
                StatusCodes.Status409Conflict);
        }

        var venue = await _unitOfWork.Venues.GetByIdAsync(
            request.VenueId,
            cancellationToken);
        if (venue == null)
        {
            return Result<EventDetailResponseDto>.Fail(
                "Venue không tồn tại.",
                StatusCodes.Status404NotFound);
        }

        if (venue.IsUnderMaintenance)
        {
            return Result<EventDetailResponseDto>.Fail(
                "Không thể tạo Event tại Venue đang bảo trì.",
                StatusCodes.Status409Conflict);
        }

        if (request.StartTime >= request.EndTime)
        {
            return Result<EventDetailResponseDto>.Fail(
                "StartTime phải nhỏ hơn EndTime.",
                StatusCodes.Status400BadRequest);
        }

        if (request.RegistrationDeadline > request.StartTime)
        {
            return Result<EventDetailResponseDto>.Fail(
                "RegistrationDeadline phải trước hoặc bằng StartTime.",
                StatusCodes.Status400BadRequest);
        }

        if (request.StartTime <= DateTime.UtcNow)
        {
            return Result<EventDetailResponseDto>.Fail(
                "StartTime phải nằm trong tương lai.",
                StatusCodes.Status400BadRequest);
        }

        if (request.Capacity > venue.Capacity)
        {
            return Result<EventDetailResponseDto>.Fail(
                $"Capacity không được vượt quá sức chứa Venue ({venue.Capacity}).",
                StatusCodes.Status400BadRequest);
        }

        var eventEntity = new Domain.Entities.Event
        {
            ClubId = club.Id,
            VenueId = venue.Id,
            Title = title,
            Description = string.IsNullOrWhiteSpace(request.Description)
                ? null
                : request.Description.Trim(),
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            RegistrationDeadline = request.RegistrationDeadline,
            Capacity = request.Capacity,
            Status = EventStatus.Draft,
            CreatedByUserId = organizerId.Value,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Events.AddAsync(eventEntity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var detail = await GetEventByIdAsync(
            eventEntity.Id,
            user,
            cancellationToken);
        return detail.Success
            ? Result<EventDetailResponseDto>.Created(detail.Data!)
            : detail;
    }

    private static int? GetUserId(ClaimsPrincipal user)
    {
        var value = user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? user.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return int.TryParse(value, out var id) && id > 0 ? id : null;
    }
}
