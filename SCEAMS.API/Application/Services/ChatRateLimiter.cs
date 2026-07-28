using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using SCEAMS.Application.Common;
using SCEAMS.Application.DTOs.Chatbot;
using SCEAMS.Application.Interfaces;

namespace SCEAMS.Application.Services;

public sealed class ChatRateLimiter : IChatRateLimiter
{
    public const int MaxQuestionsPerHour = 10;

    private readonly IChatLogRepository _chatLogRepository;
    private readonly TimeProvider _timeProvider;

    public ChatRateLimiter(
        IChatLogRepository chatLogRepository,
        TimeProvider timeProvider)
    {
        _chatLogRepository = chatLogRepository;
        _timeProvider = timeProvider;
    }

    public async Task<Result> CheckAsync(
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

        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        var windowStartUtc = nowUtc.AddHours(-1);
        var count = await _chatLogRepository.CountSinceAsync(
            studentId.Value,
            windowStartUtc,
            cancellationToken);
        if (count < MaxQuestionsPerHour)
        {
            return Result.Ok();
        }

        var oldest = await _chatLogRepository.GetOldestSinceAsync(
            studentId.Value,
            windowStartUtc,
            cancellationToken);
        var retryAfterSeconds = oldest.HasValue
            ? Math.Max(1, (int)Math.Ceiling(
                (oldest.Value.AddHours(1) - nowUtc).TotalSeconds))
            : 3600;
        return Result.Fail(
            "Bạn đã đạt giới hạn 10 câu hỏi trong một giờ. Vui lòng thử lại sau.",
            StatusCodes.Status429TooManyRequests,
            new RateLimitErrorDto(retryAfterSeconds));
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
