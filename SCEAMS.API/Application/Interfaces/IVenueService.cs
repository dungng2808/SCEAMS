using SCEAMS.Application.Common;
using SCEAMS.Application.DTOs;

namespace SCEAMS.Application.Interfaces;

public interface IVenueService
{
    Task<Result<VenueResponseDto>> CreateVenueAsync(
        CreateVenueRequestDto request,
        CancellationToken cancellationToken = default);

    Task<Result<VenueResponseDto>> UpdateVenueAsync(
        int id,
        UpdateVenueRequestDto request,
        CancellationToken cancellationToken = default);

    Task<Result<VenueResponseDto>> UpdateMaintenanceAsync(
        int id,
        UpdateVenueMaintenanceRequestDto request,
        CancellationToken cancellationToken = default);

    Task<Result> DeleteVenueAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<Result<PagedResult<VenueResponseDto>>> GetVenuesAsync(
        string? search,
        bool? maintenance,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}
