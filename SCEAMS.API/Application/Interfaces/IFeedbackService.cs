using System.Security.Claims;
using SCEAMS.Application.Common;
using SCEAMS.Application.DTOs;

namespace SCEAMS.Application.Interfaces;

public interface IFeedbackService
{
    Task<Result<FeedbackResponseDto>> CreateAsync(
        int eventId,
        CreateFeedbackRequestDto request,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default);
}
