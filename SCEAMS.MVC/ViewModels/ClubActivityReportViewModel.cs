namespace SCEAMS.MVC.ViewModels;

public sealed class ClubActivityReportViewModel
{
    public DateTime? From { get; init; }
    public DateTime? To { get; init; }
    public string? ErrorMessage { get; init; }
    public IReadOnlyList<ClubActivityReportItemViewModel> Items { get; init; } = [];
}

public sealed class ClubActivityReportItemViewModel
{
    public int ClubId { get; init; }
    public string ClubName { get; init; } = string.Empty;
    public int EventCount { get; init; }
    public int RegistrationCount { get; init; }
    public int AttendanceCount { get; init; }
    public decimal AverageRating { get; init; }
    public decimal AttendanceRate => RegistrationCount == 0
        ? 0
        : Math.Round(AttendanceCount * 100m / RegistrationCount, 2);
}
