using SCEAMS.Application.Reports;

namespace SCEAMS.Application.Interfaces;

public interface IReportRepository
{
    Task<IReadOnlyList<ReportEventSnapshot>> GetEventSnapshotsAsync(
        DateTime? fromUtc,
        DateTime? toUtcExclusive,
        int? organizerId,
        CancellationToken cancellationToken = default);
}
