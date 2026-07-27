namespace SCEAMS.MVC.Models.Api;

public sealed class ClubActivityReportApiResponse
{
    public DateTime? From { get; init; }
    public DateTime? To { get; init; }
    public IReadOnlyList<ClubActivityReportItemApiResponse> Items { get; init; } = [];
}

public sealed class ClubActivityReportItemApiResponse
{
    public int ClubId { get; init; }
    public string ClubName { get; init; } = string.Empty;
    public int EventCount { get; init; }
    public int RegistrationCount { get; init; }
    public int AttendanceCount { get; init; }
    public decimal AverageRating { get; init; }
}
