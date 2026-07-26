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

    Task<Result<FeedbackSummaryResponseDto>> GetSummaryAsync(
        int eventId,
        int page,
        int pageSize,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default);
}
