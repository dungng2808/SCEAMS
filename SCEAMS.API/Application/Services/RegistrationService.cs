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

    private static int? GetUserId(ClaimsPrincipal user)
    {
        var value = user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? user.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return int.TryParse(value, out var id) && id > 0 ? id : null;
    }
}
