using System.Security.Claims;
using SCEAMS.Application.Common;

namespace SCEAMS.Application.Interfaces;

public interface IChatRateLimiter
{
    Task<Result> CheckAsync(
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default);
}
