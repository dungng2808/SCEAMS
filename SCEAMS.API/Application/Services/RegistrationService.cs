using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using SCEAMS.Application.Common;
using SCEAMS.Application.DTOs;
using SCEAMS.Application.Interfaces;
using SCEAMS.Domain.Enums;

namespace SCEAMS.Application.Services;

public sealed class RegistrationService : IRegistrationService
{
    private readonly IUnitOfWork _unitOfWork;

    public RegistrationService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<RegistrationResponseDto>> CreateAsync(
        CreateRegistrationRequestDto request,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default)
    {
        if (!user.IsInRole(nameof(UserRole.Student)))
        {
            return Result<RegistrationResponseDto>.Fail(
                "Chỉ Student mới có thể đăng ký Event.",
                StatusCodes.Status403Forbidden);
        }

        var studentId = GetUserId(user);
        if (!studentId.HasValue)
        {
            return Result<RegistrationResponseDto>.Fail(
                "Không xác định được Student từ token.",
                StatusCodes.Status401Unauthorized);
        }

        await using var transaction = await _unitOfWork.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);

        var eventEntity = await _unitOfWork.Events.GetByIdAsync(
            request.EventId,
            cancellationToken);
        if (eventEntity == null)
        {
            return Result<RegistrationResponseDto>.Fail(
                "Event không tồn tại.",
                StatusCodes.Status404NotFound);
        }

        if (eventEntity.Status != EventStatus.Approved)
        {
            return Result<RegistrationResponseDto>.Fail(
                "Chỉ Event Approved mới có thể đăng ký.",
                StatusCodes.Status409Conflict);
        }

        if (eventEntity.RegistrationDeadline <= DateTime.UtcNow)
        {
            return Result<RegistrationResponseDto>.Fail(
                "Đã quá hạn đăng ký Event.",
                StatusCodes.Status409Conflict);
        }

        var existing = await _unitOfWork.Registrations
            .GetByStudentAndEventAsync(studentId.Value, request.EventId, cancellationToken);
        if (existing != null)
        {
            return Result<RegistrationResponseDto>.Fail(
                "Student đã đăng ký Event này trước đó.",
                StatusCodes.Status409Conflict);
        }

        var registeredCount = await _unitOfWork.Registrations
            .CountActiveForEventAsync(request.EventId, cancellationToken);
        if (registeredCount >= eventEntity.Capacity)
        {
            return Result<RegistrationResponseDto>.Fail(
                "Event đã hết chỗ đăng ký.",
                StatusCodes.Status409Conflict);
        }

        var registration = new Domain.Entities.Registration
        {
            StudentId = studentId.Value,
            EventId = request.EventId,
            Status = RegistrationStatus.Confirmed,
            RegisteredAt = DateTime.UtcNow
        };
        await _unitOfWork.Registrations.AddAsync(registration, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        registeredCount++;
        return Result<RegistrationResponseDto>.Created(
            new RegistrationResponseDto
            {
                Id = registration.Id,
                EventId = eventEntity.Id,
                EventTitle = eventEntity.Title,
                Status = registration.Status,
                RegisteredAt = registration.RegisteredAt,
                RegisteredCount = registeredCount,
                SlotsRemaining = Math.Max(0, eventEntity.Capacity - registeredCount)
            });
    }

    public async Task<Result<RegistrationResponseDto>> CancelAsync(
        int registrationId,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default)
    {
        if (!user.IsInRole(nameof(UserRole.Student)))
        {
            return Result<RegistrationResponseDto>.Fail(
                "Chỉ Student mới có thể hủy registration.",
                StatusCodes.Status403Forbidden);
        }

        var studentId = GetUserId(user);
        if (!studentId.HasValue)
        {
            return Result<RegistrationResponseDto>.Fail(
                "Không xác định được Student từ token.",
                StatusCodes.Status401Unauthorized);
        }

        var registration = await _unitOfWork.Registrations.GetByIdAsync(
            registrationId,
            cancellationToken);
        if (registration == null || registration.StudentId != studentId.Value)
        {
            return Result<RegistrationResponseDto>.Fail(
                "Registration không tồn tại.",
                StatusCodes.Status404NotFound);
        }

        var eventEntity = await _unitOfWork.Events.GetByIdAsync(
            registration.EventId,
            cancellationToken);
        if (eventEntity == null)
        {
            return Result<RegistrationResponseDto>.Fail(
                "Event của registration không tồn tại.",
                StatusCodes.Status404NotFound);
        }

        if (registration.Status != RegistrationStatus.Confirmed)
        {
            return Result<RegistrationResponseDto>.Fail(
                "Chỉ registration Confirmed mới có thể hủy.",
                StatusCodes.Status409Conflict);
        }

        if (DateTime.UtcNow > eventEntity.StartTime.AddHours(-24))
        {
            return Result<RegistrationResponseDto>.Fail(
                "Chỉ có thể hủy registration trước ít nhất 24 giờ so với StartTime.",
                StatusCodes.Status409Conflict);
        }

        registration.Status = RegistrationStatus.CancelledByStudent;
        registration.CancelledAt = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var registeredCount = await _unitOfWork.Events
            .GetConfirmedRegistrationCountAsync(eventEntity.Id, cancellationToken);
        return Result<RegistrationResponseDto>.Ok(
            new RegistrationResponseDto
            {
                Id = registration.Id,
                EventId = eventEntity.Id,
                EventTitle = eventEntity.Title,
                Status = registration.Status,
                RegisteredAt = registration.RegisteredAt,
                CancelledAt = registration.CancelledAt,
                RegisteredCount = registeredCount,
                SlotsRemaining = Math.Max(0, eventEntity.Capacity - registeredCount)
            });
    }

    public async Task<Result<PagedResult<RegistrationHistoryItemDto>>> GetMyHistoryAsync(
        string? status,
        int page,
        int pageSize,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default)
    {
        if (!user.IsInRole(nameof(UserRole.Student)))
        {
            return Result<PagedResult<RegistrationHistoryItemDto>>.Fail(
                "Chỉ Student mới có thể xem lịch sử đăng ký.",
                StatusCodes.Status403Forbidden);
        }

        var studentId = GetUserId(user);
        if (!studentId.HasValue)
        {
            return Result<PagedResult<RegistrationHistoryItemDto>>.Fail(
                "Không xác định được Student từ token.",
                StatusCodes.Status401Unauthorized);
        }

        RegistrationStatus? parsedStatus = null;
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!Enum.TryParse<RegistrationStatus>(status, true, out var statusValue))
            {
                return Result<PagedResult<RegistrationHistoryItemDto>>.Fail(
                    "Status registration không hợp lệ.",
                    StatusCodes.Status400BadRequest);
            }

            parsedStatus = statusValue;
        }

        var normalizedPage = Math.Max(page, 1);
        var normalizedPageSize = Math.Clamp(pageSize, 1, 50);
        var result = await _unitOfWork.Registrations.GetForStudentAsync(
            studentId.Value,
            parsedStatus,
            normalizedPage,
            normalizedPageSize,
            cancellationToken);
        var items = result.Items.Select(registration =>
            new RegistrationHistoryItemDto
            {
                Id = registration.Id,
                EventId = registration.EventId,
                EventTitle = registration.Event.Title,
                EventStatus = registration.Event.Status,
                StartTime = registration.Event.StartTime,
                EndTime = registration.Event.EndTime,
                RegistrationStatus = registration.Status,
                RegisteredAt = registration.RegisteredAt,
                CancelledAt = registration.CancelledAt,
                IsAttended = registration.Attendance is not null ||
                              registration.Status == RegistrationStatus.Attended,
                CheckInTime = registration.Attendance?.CheckInTime
            }).ToList();

        return Result<PagedResult<RegistrationHistoryItemDto>>.Ok(
            new PagedResult<RegistrationHistoryItemDto>(items, result.TotalItems));
    }

    public async Task<Result<PagedResult<EventRegistrationListItemDto>>> GetEventRegistrationsAsync(
        int eventId,
        string? status,
        string? search,
        int page,
        int pageSize,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default)
    {
        var isAdmin = user.IsInRole(nameof(UserRole.Admin));
        var isOrganizer = user.IsInRole(nameof(UserRole.Organizer));
        if (!isAdmin && !isOrganizer)
        {
            return Result<PagedResult<EventRegistrationListItemDto>>.Fail(
                "Chỉ Admin hoặc Organizer mới có thể xem danh sách registration.",
                StatusCodes.Status403Forbidden);
        }

        var eventEntity = await _unitOfWork.Events.GetByIdWithDetailsAsync(
            eventId,
            cancellationToken);
        if (eventEntity == null)
        {
            return Result<PagedResult<EventRegistrationListItemDto>>.Fail(
                "Event không tồn tại.",
                StatusCodes.Status404NotFound);
        }

        if (isOrganizer)
        {
            var organizerId = GetUserId(user);
            var ownsEvent = organizerId.HasValue &&
                            (eventEntity.CreatedByUserId == organizerId.Value ||
                             eventEntity.Club.CreatedByUserId == organizerId.Value);
            if (!ownsEvent)
            {
                return Result<PagedResult<EventRegistrationListItemDto>>.Fail(
                    "Organizer chỉ được xem registration của Event thuộc quyền phụ trách.",
                    StatusCodes.Status403Forbidden);
            }
        }

        RegistrationStatus? parsedStatus = null;
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!Enum.TryParse<RegistrationStatus>(status, true, out var statusValue))
            {
                return Result<PagedResult<EventRegistrationListItemDto>>.Fail(
                    "Status registration không hợp lệ.",
                    StatusCodes.Status400BadRequest);
            }

            parsedStatus = statusValue;
        }

        var normalizedPage = Math.Max(page, 1);
        var normalizedPageSize = Math.Clamp(pageSize, 1, 50);
        var result = await _unitOfWork.Registrations.GetForEventAsync(
            eventId,
            parsedStatus,
            search,
            normalizedPage,
            normalizedPageSize,
            cancellationToken);
        var items = result.Items.Select(registration =>
            new EventRegistrationListItemDto
            {
                Id = registration.Id,
                StudentCode = registration.Student.StudentCode ?? string.Empty,
                StudentName = registration.Student.FullName,
                EventStartTime = registration.Event.StartTime,
                EventEndTime = registration.Event.EndTime,
                Status = registration.Status,
                RegisteredAt = registration.RegisteredAt,
                CancelledAt = registration.CancelledAt,
                IsAttended = registration.Attendance is not null ||
                              registration.Status == RegistrationStatus.Attended,
                CheckInTime = registration.Attendance?.CheckInTime
                ,CheckedInByUserId = registration.Attendance?.CheckedInByUserId
                ,CheckedInByUserName = registration.Attendance?.CheckedInByUser?.FullName
            }).ToList();

        return Result<PagedResult<EventRegistrationListItemDto>>.Ok(
            new PagedResult<EventRegistrationListItemDto>(items, result.TotalItems));
    }

    public async Task<Result<CheckInResponseDto>> CheckInAsync(
        int registrationId,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default)
    {
        if (!user.IsInRole(nameof(UserRole.Organizer)))
        {
            return Result<CheckInResponseDto>.Fail(
                "Chỉ Organizer mới có thể điểm danh.",
                StatusCodes.Status403Forbidden);
        }

        var organizerId = GetUserId(user);
        if (!organizerId.HasValue)
        {
            return Result<CheckInResponseDto>.Fail(
                "Không xác định được Organizer từ token.",
                StatusCodes.Status401Unauthorized);
        }

        await using var transaction = await _unitOfWork.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);
        var registration = await _unitOfWork.Registrations.GetByIdAsync(
            registrationId,
            cancellationToken);
        if (registration == null)
        {
            return Result<CheckInResponseDto>.Fail(
                "Registration không tồn tại.",
                StatusCodes.Status404NotFound);
        }

        var eventEntity = await _unitOfWork.Events.GetByIdWithDetailsAsync(
            registration.EventId,
            cancellationToken);
        if (eventEntity == null)
        {
            return Result<CheckInResponseDto>.Fail(
                "Event của registration không tồn tại.",
                StatusCodes.Status404NotFound);
        }

        var ownsEvent = eventEntity.CreatedByUserId == organizerId.Value ||
                        eventEntity.Club.CreatedByUserId == organizerId.Value;
        if (!ownsEvent)
        {
            return Result<CheckInResponseDto>.Fail(
                "Organizer chỉ được điểm danh Event thuộc quyền phụ trách.",
                StatusCodes.Status403Forbidden);
        }

        var now = DateTime.UtcNow;
        if (eventEntity.Status != EventStatus.Ongoing ||
            now < eventEntity.StartTime ||
            now > eventEntity.EndTime)
        {
            return Result<CheckInResponseDto>.Fail(
                "Chỉ có thể điểm danh khi Event đang diễn ra.",
                StatusCodes.Status409Conflict);
        }

        if (registration.Status != RegistrationStatus.Confirmed)
        {
            return Result<CheckInResponseDto>.Fail(
                "Chỉ registration Confirmed mới có thể điểm danh.",
                StatusCodes.Status409Conflict);
        }

        var alreadyCheckedIn = await _unitOfWork.Attendances.AnyAsync(
            attendance => attendance.RegistrationId == registrationId,
            cancellationToken);
        if (alreadyCheckedIn)
        {
            return Result<CheckInResponseDto>.Fail(
                "Registration này đã được điểm danh.",
                StatusCodes.Status409Conflict);
        }

        var attendance = new Domain.Entities.Attendance
        {
            RegistrationId = registrationId,
            CheckInTime = now,
            CheckedInByUserId = organizerId.Value
        };
        registration.Status = RegistrationStatus.Attended;
        await _unitOfWork.Attendances.AddAsync(attendance, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Result<CheckInResponseDto>.Ok(new CheckInResponseDto
        {
            RegistrationId = registrationId,
            EventId = registration.EventId,
            Status = registration.Status,
            CheckInTime = attendance.CheckInTime,
            CheckedInByUserId = attendance.CheckedInByUserId
        });
    }

    private static int? GetUserId(ClaimsPrincipal user)
    {
        var value = user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? user.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return int.TryParse(value, out var id) && id > 0 ? id : null;
    }
}
