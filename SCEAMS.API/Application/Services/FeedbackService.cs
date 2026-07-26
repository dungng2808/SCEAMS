using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SCEAMS.Application.Common;
using SCEAMS.Application.DTOs;
using SCEAMS.Application.Interfaces;
using SCEAMS.Domain.Entities;
using SCEAMS.Domain.Enums;

namespace SCEAMS.Application.Services;

public sealed class FeedbackService : IFeedbackService
{
    private readonly IUnitOfWork _unitOfWork;

    public FeedbackService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<FeedbackResponseDto>> CreateAsync(
        int eventId,
        CreateFeedbackRequestDto request,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default)
    {
        if (!user.IsInRole(nameof(UserRole.Student)))
        {
            return Result<FeedbackResponseDto>.Fail(
                "Chỉ Student mới có thể gửi feedback.",
                StatusCodes.Status403Forbidden);
        }

        var studentId = GetUserId(user);
        if (!studentId.HasValue)
        {
            return Result<FeedbackResponseDto>.Fail(
                "Không xác định được Student từ token.",
                StatusCodes.Status401Unauthorized);
        }

        await using var transaction = await _unitOfWork.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);
        var eventEntity = await _unitOfWork.Events.GetByIdAsync(
            eventId,
            cancellationToken);
        if (eventEntity == null)
        {
            return Result<FeedbackResponseDto>.Fail(
                "Event không tồn tại.",
                StatusCodes.Status404NotFound);
        }

        var registration = await _unitOfWork.Registrations
            .GetByStudentAndEventAsync(studentId.Value, eventId, cancellationToken);
        if (registration == null || registration.Status != RegistrationStatus.Attended)
        {
            return Result<FeedbackResponseDto>.Fail(
                "Chỉ Student đã Attended Event mới có thể gửi feedback.",
                StatusCodes.Status409Conflict);
        }

        var existing = await _unitOfWork.Feedbacks.AnyAsync(
            feedback => feedback.EventId == eventId &&
                        feedback.StudentId == studentId.Value,
            cancellationToken);
        if (existing)
        {
            return Result<FeedbackResponseDto>.Fail(
                "Student đã gửi feedback cho Event này.",
                StatusCodes.Status409Conflict);
        }

        var feedbackEntity = new Feedback
        {
            EventId = eventId,
            StudentId = studentId.Value,
            Rating = request.Rating,
            Comment = string.IsNullOrWhiteSpace(request.Comment)
                ? null
                : request.Comment.Trim(),
            CreatedAt = DateTime.UtcNow
        };
        await _unitOfWork.Feedbacks.AddAsync(feedbackEntity, cancellationToken);
        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return Result<FeedbackResponseDto>.Fail(
                "Student đã gửi feedback cho Event này.",
                StatusCodes.Status409Conflict);
        }

        return Result<FeedbackResponseDto>.Created(new FeedbackResponseDto
        {
            Id = feedbackEntity.Id,
            EventId = feedbackEntity.EventId,
            Rating = feedbackEntity.Rating,
            Comment = feedbackEntity.Comment,
            CreatedAt = feedbackEntity.CreatedAt
        });
    }

    public async Task<Result<FeedbackSummaryResponseDto>> GetSummaryAsync(
        int eventId,
        int page,
        int pageSize,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default)
    {
        var eventEntity = await _unitOfWork.Events.GetByIdWithDetailsAsync(
            eventId,
            cancellationToken);
        if (eventEntity == null)
        {
            return Result<FeedbackSummaryResponseDto>.Fail(
                "Event không tồn tại.",
                StatusCodes.Status404NotFound);
        }

        var isInternal = user.IsInRole(nameof(UserRole.Admin)) ||
                         user.IsInRole(nameof(UserRole.Staff));
        var currentUserId = GetUserId(user);
        var isOwner = currentUserId.HasValue &&
                      (eventEntity.CreatedByUserId == currentUserId.Value ||
                       eventEntity.Club.CreatedByUserId == currentUserId.Value);
        var isPublic = eventEntity.Status is
            EventStatus.Approved or EventStatus.Ongoing or EventStatus.Completed;
        if (!isInternal && !isOwner && !isPublic)
        {
            return Result<FeedbackSummaryResponseDto>.Fail(
                "Event không tồn tại.",
                StatusCodes.Status404NotFound);
        }

        var feedbacks = await _unitOfWork.Feedbacks.FindAsync(
            feedback => feedback.EventId == eventId,
            cancellationToken);
        var normalizedPage = Math.Max(page, 1);
        var normalizedPageSize = Math.Clamp(pageSize, 1, 50);
        var totalFeedback = feedbacks.Count;
        var items = feedbacks
            .OrderByDescending(feedback => feedback.CreatedAt)
            .Skip((normalizedPage - 1) * normalizedPageSize)
            .Take(normalizedPageSize)
            .Select(feedback => new FeedbackListItemDto
            {
                Id = feedback.Id,
                Rating = feedback.Rating,
                Comment = feedback.Comment,
                CreatedAt = feedback.CreatedAt
            })
            .ToList();

        return Result<FeedbackSummaryResponseDto>.Ok(new FeedbackSummaryResponseDto
        {
            EventId = eventId,
            AverageRating = totalFeedback == 0
                ? 0
                : Math.Round((decimal)feedbacks.Average(feedback => feedback.Rating), 2),
            TotalFeedback = totalFeedback,
            Items = items,
            Page = normalizedPage,
            PageSize = normalizedPageSize,
            TotalPages = totalFeedback == 0
                ? 0
                : (int)Math.Ceiling(totalFeedback / (double)normalizedPageSize)
        });
    }

    private static int? GetUserId(ClaimsPrincipal user)
    {
        var value = user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? user.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return int.TryParse(value, out var id) && id > 0 ? id : null;
    }
}
