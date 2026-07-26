using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using SCEAMS.Application.Common;
using SCEAMS.Application.DTOs.Reports;
using SCEAMS.Application.Interfaces;
using SCEAMS.Application.Reports;
using SCEAMS.Domain.Enums;

namespace SCEAMS.Application.Services;

public sealed class ReportService : IReportService
{
    private readonly IReportRepository _reportRepository;

    public ReportService(IReportRepository reportRepository)
    {
        _reportRepository = reportRepository;
    }

    public async Task<Result<EventSummaryReportDto>> GetEventSummaryAsync(
        DateTime? from,
        DateTime? to,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default)
    {
        var permission = EnsureInternalUser(user);
        if (permission is not null)
        {
            return Result<EventSummaryReportDto>.Fail(
                permission,
                StatusCodes.Status403Forbidden);
        }

        if (!TryNormalizeRange(from, to, out var range, out var rangeError))
        {
            return Result<EventSummaryReportDto>.Fail(
                rangeError!,
                StatusCodes.Status400BadRequest);
        }

        var events = await _reportRepository.GetEventSnapshotsAsync(
            range.FromUtc,
            range.ToUtcExclusive,
            organizerId: null,
            cancellationToken);
        var counts = events
            .GroupBy(item => item.EventStatus)
            .ToDictionary(group => group.Key, group => group.Count());

        return Result<EventSummaryReportDto>.Ok(new EventSummaryReportDto
        {
            From = range.FromDate,
            To = range.ToDate,
            TotalEvents = events.Count,
            Items = Enum.GetValues<EventStatus>()
                .Select(status => new EventSummaryItemDto
                {
                    Status = status.ToString(),
                    Count = counts.GetValueOrDefault(status)
                })
                .ToList()
        });
    }

    public async Task<Result<ClubActivityReportDto>> GetClubActivityAsync(
        DateTime? from,
        DateTime? to,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default)
    {
        var organizerId = GetScopeOrganizerId(user);
        if (organizerId is null && !IsInternalUser(user))
        {
            return Result<ClubActivityReportDto>.Fail(
                "Chỉ Admin, Staff hoặc Organizer mới có thể xem báo cáo hoạt động CLB.",
                StatusCodes.Status403Forbidden);
        }

        if (!TryNormalizeRange(from, to, out var range, out var rangeError))
        {
            return Result<ClubActivityReportDto>.Fail(
                rangeError!,
                StatusCodes.Status400BadRequest);
        }

        var events = await _reportRepository.GetEventSnapshotsAsync(
            range.FromUtc,
            range.ToUtcExclusive,
            organizerId,
            cancellationToken);
        var items = events
            .GroupBy(item => new { item.ClubId, item.ClubName })
            .Select(group => new ClubActivityItemDto
            {
                ClubId = group.Key.ClubId,
                ClubName = group.Key.ClubName,
                EventCount = group.Count(),
                RegistrationCount = group.Sum(item => item.RegisteredCount),
                AttendanceCount = group.Sum(item => item.AttendedCount),
                AverageRating = group.Any(item => item.AverageRating > 0)
                    ? Math.Round(group.Where(item => item.AverageRating > 0)
                        .Average(item => item.AverageRating), 2)
                    : 0m
            })
            .OrderByDescending(item => item.EventCount)
            .ThenBy(item => item.ClubName)
            .ToList();

        return Result<ClubActivityReportDto>.Ok(new ClubActivityReportDto
        {
            From = range.FromDate,
            To = range.ToDate,
            Items = items
        });
    }

    public async Task<Result<AttendanceRateReportDto>> GetAttendanceRateAsync(
        DateTime? from,
        DateTime? to,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default)
    {
        var organizerId = GetScopeOrganizerId(user);
        if (organizerId is null && !IsInternalUser(user))
        {
            return Result<AttendanceRateReportDto>.Fail(
                "Chỉ Admin, Staff hoặc Organizer mới có thể xem báo cáo điểm danh.",
                StatusCodes.Status403Forbidden);
        }

        if (!TryNormalizeRange(from, to, out var range, out var rangeError))
        {
            return Result<AttendanceRateReportDto>.Fail(
                rangeError!,
                StatusCodes.Status400BadRequest);
        }

        var events = await _reportRepository.GetEventSnapshotsAsync(
            range.FromUtc,
            range.ToUtcExclusive,
            organizerId,
            cancellationToken);
        var items = events
            .Select(item => new AttendanceRateItemDto
            {
                EventId = item.EventId,
                EventTitle = item.EventTitle,
                ClubName = item.ClubName,
                StartTime = item.StartTime,
                Status = item.EventStatus.ToString(),
                RegisteredCount = item.RegisteredCount,
                AttendedCount = item.AttendedCount,
                AttendanceRate = item.RegisteredCount == 0
                    ? 0m
                    : Math.Round(item.AttendedCount * 100m / item.RegisteredCount, 2)
            })
            .ToList();

        return Result<AttendanceRateReportDto>.Ok(new AttendanceRateReportDto
        {
            From = range.FromDate,
            To = range.ToDate,
            Items = items
        });
    }

    public async Task<Result<VenueUsageReportDto>> GetVenueUsageAsync(
        DateTime? from,
        DateTime? to,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default)
    {
        var permission = EnsureInternalUser(user);
        if (permission is not null)
        {
            return Result<VenueUsageReportDto>.Fail(
                permission,
                StatusCodes.Status403Forbidden);
        }

        if (!TryNormalizeRange(from, to, out var range, out var rangeError))
        {
            return Result<VenueUsageReportDto>.Fail(
                rangeError!,
                StatusCodes.Status400BadRequest);
        }

        var events = await _reportRepository.GetEventSnapshotsAsync(
            range.FromUtc,
            range.ToUtcExclusive,
            organizerId: null,
            cancellationToken);
        var items = events
            .Where(item => item.EventStatus is
                EventStatus.Approved or EventStatus.Ongoing or EventStatus.Completed)
            .GroupBy(item => new
            {
                item.VenueId,
                item.VenueName,
                item.VenueLocation
            })
            .Select(group => new VenueUsageItemDto
            {
                VenueId = group.Key.VenueId,
                VenueName = group.Key.VenueName,
                Location = group.Key.VenueLocation,
                EventCount = group.Count(),
                TotalHours = Math.Round(group.Sum(item =>
                    (decimal)(item.EndTime - item.StartTime).TotalHours), 2)
            })
            .OrderByDescending(item => item.TotalHours)
            .ThenBy(item => item.VenueName)
            .ToList();

        return Result<VenueUsageReportDto>.Ok(new VenueUsageReportDto
        {
            From = range.FromDate,
            To = range.ToDate,
            Items = items
        });
    }

    private static bool IsInternalUser(ClaimsPrincipal user) =>
        user.IsInRole(nameof(UserRole.Admin)) ||
        user.IsInRole(nameof(UserRole.Staff));

    private static string? EnsureInternalUser(ClaimsPrincipal user) =>
        IsInternalUser(user)
            ? null
            : "Chỉ Admin hoặc Staff mới có thể xem báo cáo này.";

    private static int? GetScopeOrganizerId(ClaimsPrincipal user)
    {
        if (IsInternalUser(user))
        {
            return null;
        }

        if (!user.IsInRole(nameof(UserRole.Organizer)))
        {
            return null;
        }

        var value = user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? user.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return int.TryParse(value, out var id) && id > 0 ? id : null;
    }

    private static bool TryNormalizeRange(
        DateTime? from,
        DateTime? to,
        out ReportDateRange range,
        out string? error)
    {
        var fromDate = from?.Date;
        var toDate = to?.Date;
        if (fromDate.HasValue && toDate.HasValue && fromDate > toDate)
        {
            range = default;
            error = "Ngày bắt đầu phải nhỏ hơn hoặc bằng ngày kết thúc.";
            return false;
        }

        range = new ReportDateRange(
            fromDate,
            toDate,
            fromDate.HasValue
                ? DateTime.SpecifyKind(fromDate.Value, DateTimeKind.Utc)
                : null,
            toDate.HasValue
                ? DateTime.SpecifyKind(toDate.Value.AddDays(1), DateTimeKind.Utc)
                : null);
        error = null;
        return true;
    }

    private readonly record struct ReportDateRange(
        DateTime? FromDate,
        DateTime? ToDate,
        DateTime? FromUtc,
        DateTime? ToUtcExclusive);
}
