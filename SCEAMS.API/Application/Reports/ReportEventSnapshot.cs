using SCEAMS.Domain.Enums;

namespace SCEAMS.Application.Reports;

/// <summary>
/// Read-only projection used by the reporting service. It intentionally
/// contains aggregates only, never credentials or other user-sensitive data.
/// </summary>
public sealed record ReportEventSnapshot(
    int EventId,
    string EventTitle,
    EventStatus EventStatus,
    DateTime StartTime,
    DateTime EndTime,
    int ClubId,
    string ClubName,
    int VenueId,
    string VenueName,
    string VenueLocation,
    int RegisteredCount,
    int AttendedCount,
    decimal AverageRating);
