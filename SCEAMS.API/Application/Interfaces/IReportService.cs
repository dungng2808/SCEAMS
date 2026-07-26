using System.Security.Claims;
using SCEAMS.Application.Common;
using SCEAMS.Application.DTOs.Reports;

namespace SCEAMS.Application.Interfaces;

public interface IReportService
{
    Task<Result<EventSummaryReportDto>> GetEventSummaryAsync(
        DateTime? from,
        DateTime? to,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default);

    Task<Result<ClubActivityReportDto>> GetClubActivityAsync(
        DateTime? from,
        DateTime? to,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default);

    Task<Result<AttendanceRateReportDto>> GetAttendanceRateAsync(
        DateTime? from,
        DateTime? to,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default);

    Task<Result<VenueUsageReportDto>> GetVenueUsageAsync(
        DateTime? from,
        DateTime? to,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default);
}
