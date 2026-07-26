namespace SCEAMS.Application.DTOs.Reports;

public sealed class ClubActivityReportDto
{
    public DateTime? From { get; init; }
    public DateTime? To { get; init; }
    public IReadOnlyList<ClubActivityItemDto> Items { get; init; } = [];
}

public sealed class ClubActivityItemDto
{
    public int ClubId { get; init; }
    public string ClubName { get; init; } = string.Empty;
    public int EventCount { get; init; }
    public int RegistrationCount { get; init; }
    public int AttendanceCount { get; init; }
    public decimal AverageRating { get; init; }
}
