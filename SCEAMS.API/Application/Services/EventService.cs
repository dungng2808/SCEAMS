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
    private readonly INotificationClientService _notificationClientService;

    public EventService(
        IUnitOfWork unitOfWork,
        INotificationClientService notificationClientService)
    {
        _unitOfWork = unitOfWork;
        _notificationClientService = notificationClientService;
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
        CancellationToken cancellationToken = default,
        string? notificationCorrelationId = null,
        bool? notificationDelivered = null,
        string? notificationError = null)
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
        var currentRegistration = isStudent
            ? await _unitOfWork.Registrations.GetByStudentAndEventAsync(
                GetUserId(user) ?? 0,
                id,
                cancellationToken)
            : null;
        var currentRegistrationStatus = currentRegistration?.Status.ToString();
        FeedbackResponseDto? currentFeedback = null;
        if (isStudent)
        {
            var feedback = (await _unitOfWork.Feedbacks.FindAsync(
                item => item.EventId == id &&
                        item.StudentId == (GetUserId(user) ?? 0),
                cancellationToken)).FirstOrDefault();
            if (feedback is not null)
            {
                currentFeedback = new FeedbackResponseDto
                {
                    Id = feedback.Id,
                    EventId = feedback.EventId,
                    Rating = feedback.Rating,
                    Comment = feedback.Comment,
                    CreatedAt = feedback.CreatedAt
                };
            }
        }
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
            CurrentRegistrationStatus = currentRegistrationStatus,
            CurrentRegistrationId = currentRegistration?.Id,
            CanFeedback = isStudent &&
                          currentRegistrationStatus == RegistrationStatus.Attended.ToString() &&
                          currentFeedback is null,
            CurrentFeedback = currentFeedback,
            NotificationCorrelationId = notificationCorrelationId,
            NotificationDelivered = notificationDelivered,
            NotificationError = notificationError,
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
                              slotsRemaining > 0 &&
                              currentRegistrationStatus is null
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

    public async Task<Result<EventDetailResponseDto>> UpdateEventAsync(
        int id,
        UpdateEventRequestDto request,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default)
    {
        var eventEntity = await _unitOfWork.Events.GetByIdAsync(id, cancellationToken);
        if (eventEntity == null)
        {
            return Result<EventDetailResponseDto>.Fail(
                "Event không tồn tại.",
                StatusCodes.Status404NotFound);
        }

        var currentUserId = GetUserId(user);
        var isAdminOrStaff = user.IsInRole(nameof(UserRole.Admin)) ||
                             user.IsInRole(nameof(UserRole.Staff));
        var isOrganizer = user.IsInRole(nameof(UserRole.Organizer));
        var club = await _unitOfWork.Clubs.GetByIdWithDetailsAsync(
            eventEntity.ClubId,
            cancellationToken);
        var isOwner = currentUserId.HasValue &&
                      (eventEntity.CreatedByUserId == currentUserId.Value ||
                       (club?.CreatedByUserId == currentUserId.Value));

        if (!isAdminOrStaff && (!isOrganizer || !isOwner))
        {
            return Result<EventDetailResponseDto>.Fail(
                "Bạn không có quyền sửa Event này.",
                StatusCodes.Status403Forbidden);
        }

        if (eventEntity.Status is EventStatus.Completed or EventStatus.Cancelled)
        {
            return Result<EventDetailResponseDto>.Fail(
                "Không thể sửa Event đã Completed hoặc Cancelled.",
                StatusCodes.Status409Conflict);
        }

        if (isOrganizer && !isAdminOrStaff && eventEntity.Status != EventStatus.Draft)
        {
            return Result<EventDetailResponseDto>.Fail(
                "Organizer chỉ được sửa Event ở trạng thái Draft.",
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
                "Không thể chuyển Event tới Venue đang bảo trì.",
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

        if (request.Capacity > venue.Capacity)
        {
            return Result<EventDetailResponseDto>.Fail(
                $"Capacity không được vượt quá sức chứa Venue ({venue.Capacity}).",
                StatusCodes.Status400BadRequest);
        }

        var registeredCount = await _unitOfWork.Events
            .GetConfirmedRegistrationCountAsync(id, cancellationToken);
        if (request.Capacity < registeredCount)
        {
            return Result<EventDetailResponseDto>.Fail(
                $"Capacity không được nhỏ hơn số đăng ký hợp lệ hiện tại ({registeredCount}).",
                StatusCodes.Status409Conflict);
        }

        eventEntity.Title = request.Title.Trim();
        eventEntity.Description = string.IsNullOrWhiteSpace(request.Description)
            ? null
            : request.Description.Trim();
        eventEntity.VenueId = request.VenueId;
        eventEntity.StartTime = request.StartTime;
        eventEntity.EndTime = request.EndTime;
        eventEntity.RegistrationDeadline = request.RegistrationDeadline;
        eventEntity.Capacity = request.Capacity;
        eventEntity.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return await GetEventByIdAsync(id, user, cancellationToken);
    }

    public async Task<Result<EventDetailResponseDto>> SubmitEventAsync(
        int id,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default)
    {
        var eventEntity = await _unitOfWork.Events.GetByIdAsync(id, cancellationToken);
        if (eventEntity == null)
        {
            return Result<EventDetailResponseDto>.Fail(
                "Event không tồn tại.",
                StatusCodes.Status404NotFound);
        }

        var currentUserId = GetUserId(user);
        var club = await _unitOfWork.Clubs.GetByIdWithDetailsAsync(
            eventEntity.ClubId,
            cancellationToken);
        var isOwner = currentUserId.HasValue &&
                      (eventEntity.CreatedByUserId == currentUserId.Value ||
                       club?.CreatedByUserId == currentUserId.Value);
        if (!isOwner)
        {
            return Result<EventDetailResponseDto>.Fail(
                "Chỉ Organizer sở hữu Event mới có thể gửi duyệt.",
                StatusCodes.Status403Forbidden);
        }

        if (eventEntity.Status != EventStatus.Draft)
        {
            return Result<EventDetailResponseDto>.Fail(
                $"Chỉ Event Draft mới có thể gửi duyệt. Trạng thái hiện tại: {eventEntity.Status}.",
                StatusCodes.Status409Conflict);
        }

        var venue = await _unitOfWork.Venues.GetByIdAsync(
            eventEntity.VenueId,
            cancellationToken);
        var validationMessage = ValidateEventForSubmission(eventEntity, venue);
        if (validationMessage != null)
        {
            return Result<EventDetailResponseDto>.Fail(
                validationMessage,
                StatusCodes.Status400BadRequest);
        }

        eventEntity.Status = EventStatus.PendingApproval;
        eventEntity.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return await GetEventByIdAsync(id, user, cancellationToken);
    }

    public async Task<Result<PagedResult<EventListResponseDto>>> GetPendingApprovalEventsAsync(
        int? clubId,
        int? venueId,
        DateTime? from,
        DateTime? to,
        int page,
        int pageSize,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default)
    {
        var isAdminOrStaff = user.IsInRole(nameof(UserRole.Admin)) ||
                             user.IsInRole(nameof(UserRole.Staff));
        if (!isAdminOrStaff)
        {
            return Result<PagedResult<EventListResponseDto>>.Fail(
                "Chỉ Admin hoặc Staff mới có thể xem queue duyệt Event.",
                StatusCodes.Status403Forbidden);
        }

        var query = GetEventsQuery(user)
            .Where(eventItem => eventItem.Status == EventStatus.PendingApproval);
        if (clubId is > 0)
        {
            query = query.Where(eventItem => eventItem.ClubId == clubId.Value);
        }

        if (venueId is > 0)
        {
            query = query.Where(eventItem => eventItem.VenueId == venueId.Value);
        }

        if (from.HasValue)
        {
            query = query.Where(eventItem => eventItem.StartTime >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(eventItem => eventItem.StartTime < to.Value.Date.AddDays(1));
        }

        var totalItems = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
            .CountAsync(query, cancellationToken);
        var normalizedPage = Math.Max(page, 1);
        var normalizedPageSize = Math.Clamp(pageSize, 1, 50);
        var items = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
            .ToListAsync(
                query.OrderBy(eventItem => eventItem.StartTime)
                    .Skip((normalizedPage - 1) * normalizedPageSize)
                    .Take(normalizedPageSize),
                cancellationToken);

        return Result<PagedResult<EventListResponseDto>>.Ok(
            new PagedResult<EventListResponseDto>(items, totalItems));
    }

    public async Task<Result<EventDetailResponseDto>> ApproveEventAsync(
        int id,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default)
    {
        var eventEntity = await _unitOfWork.Events.GetByIdAsync(id, cancellationToken);
        if (eventEntity == null)
        {
            return Result<EventDetailResponseDto>.Fail(
                "Event không tồn tại.",
                StatusCodes.Status404NotFound);
        }

        var reviewerId = GetUserId(user);
        if (!reviewerId.HasValue)
        {
            return Result<EventDetailResponseDto>.Fail(
                "Không xác định được người duyệt từ token.",
                StatusCodes.Status401Unauthorized);
        }

        if (eventEntity.Status != EventStatus.PendingApproval)
        {
            return Result<EventDetailResponseDto>.Fail(
                $"Chỉ Event PendingApproval mới có thể duyệt. Trạng thái hiện tại: {eventEntity.Status}.",
                StatusCodes.Status409Conflict);
        }

        var venue = await _unitOfWork.Venues.GetByIdAsync(
            eventEntity.VenueId,
            cancellationToken);
        if (venue == null)
        {
            return Result<EventDetailResponseDto>.Fail(
                "Venue của Event không tồn tại.",
                StatusCodes.Status404NotFound);
        }

        if (venue.IsUnderMaintenance)
        {
            return Result<EventDetailResponseDto>.Fail(
                "Không thể duyệt Event tại Venue đang bảo trì.",
                StatusCodes.Status409Conflict);
        }

        var conflicts = await _unitOfWork.Events.GetVenueConflictsAsync(
            eventEntity.VenueId,
            eventEntity.StartTime,
            eventEntity.EndTime,
            eventEntity.Id,
            cancellationToken);
        if (conflicts.Count > 0)
        {
            var conflictDtos = conflicts.Select(conflict =>
                new EventApprovalConflictDto
                {
                    EventId = conflict.Id,
                    Title = conflict.Title,
                    VenueName = venue.Name,
                    Status = conflict.Status.ToString(),
                    StartTime = conflict.StartTime,
                    EndTime = conflict.EndTime
                }).ToList();
            return Result<EventDetailResponseDto>.Fail(
                "Không thể duyệt vì Event bị trùng Venue với lịch Approved/Ongoing.",
                StatusCodes.Status409Conflict,
                conflictDtos);
        }

        eventEntity.Status = EventStatus.Approved;
        eventEntity.ApprovedByUserId = reviewerId.Value;
        eventEntity.ApprovedAt = DateTime.UtcNow;
        eventEntity.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        var notification = await _notificationClientService
            .NotifyEventStatusChangedAsync(
                eventEntity.Id,
                eventEntity.Title,
                EventStatus.Approved,
                eventEntity.CreatedByUserId,
                cancellationToken);
        return await GetEventByIdAsync(
            id,
            user,
            cancellationToken,
            notification.CorrelationId,
            notification.Success,
            notification.ErrorMessage);
    }

    public async Task<Result<EventDetailResponseDto>> RejectEventAsync(
        int id,
        RejectEventRequestDto request,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default)
    {
        var reviewerId = GetUserId(user);
        if (!reviewerId.HasValue)
        {
            return Result<EventDetailResponseDto>.Fail(
                "Không xác định được người duyệt từ token.",
                StatusCodes.Status401Unauthorized);
        }

        var eventEntity = await _unitOfWork.Events.GetByIdAsync(id, cancellationToken);
        if (eventEntity == null)
        {
            return Result<EventDetailResponseDto>.Fail(
                "Event không tồn tại.",
                StatusCodes.Status404NotFound);
        }

        if (eventEntity.Status != EventStatus.PendingApproval)
        {
            return Result<EventDetailResponseDto>.Fail(
                $"Chỉ Event PendingApproval mới có thể từ chối. Trạng thái hiện tại: {eventEntity.Status}.",
                StatusCodes.Status409Conflict);
        }

        eventEntity.Status = EventStatus.Rejected;
        eventEntity.RejectionReason = request.Reason.Trim();
        eventEntity.ApprovedByUserId = reviewerId.Value;
        eventEntity.ApprovedAt = DateTime.UtcNow;
        eventEntity.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await GetEventByIdAsync(id, user, cancellationToken);
    }

    public async Task<Result<EventDetailResponseDto>> CancelEventAsync(
        int id,
        CancelEventRequestDto request,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default)
    {
        var eventEntity = await _unitOfWork.Events.GetByIdAsync(id, cancellationToken);
        if (eventEntity == null)
        {
            return Result<EventDetailResponseDto>.Fail(
                "Event không tồn tại.",
                StatusCodes.Status404NotFound);
        }

        var isAdminOrStaff = user.IsInRole(nameof(UserRole.Admin)) ||
                             user.IsInRole(nameof(UserRole.Staff));
        var currentUserId = GetUserId(user);
        var club = await _unitOfWork.Clubs.GetByIdWithDetailsAsync(
            eventEntity.ClubId,
            cancellationToken);
        var isOwner = currentUserId.HasValue &&
                      (eventEntity.CreatedByUserId == currentUserId.Value ||
                       club?.CreatedByUserId == currentUserId.Value);
        if (!isAdminOrStaff &&
            !(user.IsInRole(nameof(UserRole.Organizer)) && isOwner))
        {
            return Result<EventDetailResponseDto>.Fail(
                "Chỉ Organizer sở hữu Event hoặc Admin/Staff mới có thể hủy Event.",
                StatusCodes.Status403Forbidden);
        }

        if (eventEntity.Status is EventStatus.Completed or EventStatus.Cancelled)
        {
            return Result<EventDetailResponseDto>.Fail(
                $"Event ở trạng thái {eventEntity.Status} không thể hủy thêm.",
                StatusCodes.Status409Conflict);
        }

        if (!isAdminOrStaff && eventEntity.StartTime <= DateTime.UtcNow)
        {
            return Result<EventDetailResponseDto>.Fail(
                "Organizer chỉ có thể hủy Event trước thời điểm bắt đầu.",
                StatusCodes.Status409Conflict);
        }

        eventEntity.Status = EventStatus.Cancelled;
        eventEntity.CancellationReason = request.Reason.Trim();
        eventEntity.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        var notification = await _notificationClientService
            .NotifyEventStatusChangedAsync(
                eventEntity.Id,
                eventEntity.Title,
                EventStatus.Cancelled,
                eventEntity.CreatedByUserId,
                cancellationToken);

        return await GetEventByIdAsync(
            id,
            user,
            cancellationToken,
            notification.CorrelationId,
            notification.Success,
            notification.ErrorMessage);
    }

    private static string? ValidateEventForSubmission(
        Domain.Entities.Event eventEntity,
        Domain.Entities.Venue? venue)
    {
        if (string.IsNullOrWhiteSpace(eventEntity.Title))
        {
            return "Event phải có tiêu đề trước khi gửi duyệt.";
        }

        if (venue == null || venue.IsUnderMaintenance)
        {
            return "Venue phải tồn tại và không ở trạng thái bảo trì.";
        }

        if (eventEntity.StartTime >= eventEntity.EndTime)
        {
            return "StartTime phải nhỏ hơn EndTime.";
        }

        if (eventEntity.RegistrationDeadline > eventEntity.StartTime)
        {
            return "RegistrationDeadline phải trước hoặc bằng StartTime.";
        }

        if (eventEntity.Capacity <= 0 || eventEntity.Capacity > venue.Capacity)
        {
            return "Capacity phải lớn hơn 0 và không vượt sức chứa Venue.";
        }

        return null;
    }

    private static int? GetUserId(ClaimsPrincipal user)
    {
        var value = user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? user.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return int.TryParse(value, out var id) && id > 0 ? id : null;
    }
}
