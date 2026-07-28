using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using SCEAMS.Application.Common;
using SCEAMS.Application.DTOs.Chatbot;
using SCEAMS.Application.Interfaces;

namespace SCEAMS.Application.Services;

public sealed class ChatHistoryService : IChatHistoryService
{
    private readonly IChatLogRepository _chatLogRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    public ChatHistoryService(
        IChatLogRepository chatLogRepository,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _chatLogRepository = chatLogRepository;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
    }

    public async Task<Result> SaveAsync(
        string question,
        string answer,
        IReadOnlyList<int> relatedEventIds,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default)
    {
        var studentId = GetStudentId(user);
        if (!studentId.HasValue)
        {
            return Result.Fail(
                "Không xác định được Student từ token.",
                StatusCodes.Status401Unauthorized);
        }

        await _chatLogRepository.AddAsync(new Domain.Entities.ChatLog
        {
            StudentId = studentId.Value,
            Question = question.Trim(),
            AnswerText = answer.Trim(),
            RelatedEventIds = JsonSerializer.Serialize(
                relatedEventIds.Distinct().Take(10).ToArray()),
            CreatedAt = _timeProvider.GetUtcNow().UtcDateTime
        }, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Ok();
    }

    public async Task<Result<ChatHistoryPageDto>> GetForCurrentStudentAsync(
        ClaimsPrincipal user,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var studentId = GetStudentId(user);
        if (!studentId.HasValue)
        {
            return Result<ChatHistoryPageDto>.Fail(
                "Không xác định được Student từ token.",
                StatusCodes.Status401Unauthorized);
        }

        var normalizedPage = Math.Max(page, 1);
        var normalizedPageSize = Math.Clamp(pageSize, 1, 50);
        var result = await _chatLogRepository.GetForStudentAsync(
            studentId.Value,
            normalizedPage,
            normalizedPageSize,
            cancellationToken);
        var totalPages = result.TotalItems == 0
            ? 0
            : (int)Math.Ceiling(result.TotalItems / (double)normalizedPageSize);
        return Result<ChatHistoryPageDto>.Ok(new ChatHistoryPageDto
        {
            Page = normalizedPage,
            PageSize = normalizedPageSize,
            TotalItems = result.TotalItems,
            TotalPages = totalPages,
            Items = result.Items.Select(MapItem).ToList()
        });
    }

    private static ChatHistoryItemDto MapItem(Domain.Entities.ChatLog chatLog)
    {
        IReadOnlyList<int> eventIds;
        try
        {
            eventIds = JsonSerializer.Deserialize<List<int>>(
                chatLog.RelatedEventIds) ?? [];
        }
        catch (JsonException)
        {
            eventIds = [];
        }

        return new ChatHistoryItemDto
        {
            Id = chatLog.Id,
            Question = chatLog.Question,
            AnswerText = chatLog.AnswerText,
            RelatedEventIds = eventIds,
            CreatedAt = chatLog.CreatedAt
        };
    }

    private static int? GetStudentId(ClaimsPrincipal user)
    {
        var role = user.FindFirstValue(ClaimTypes.Role)
            ?? user.FindFirstValue("role");
        if (!string.Equals(role, "Student", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var value = user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? user.FindFirstValue("sub");
        return int.TryParse(value, out var studentId) && studentId > 0
            ? studentId
            : null;
    }
}
