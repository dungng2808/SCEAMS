using SCEAMS.Application.Common;
using SCEAMS.Application.DTOs;

namespace SCEAMS.Application.Interfaces;

public interface IVenueService
{
    Task<Result<PagedResult<VenueResponseDto>>> GetVenuesAsync(
        string? search,
        bool? maintenance,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}
