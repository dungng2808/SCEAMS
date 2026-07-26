namespace SCEAMS.MVC.ViewModels;

public sealed class PendingClubsViewModel
{
    public IReadOnlyList<ClubListItemViewModel> PendingClubs { get; init; } = [];
    public bool IsForbidden { get; init; }
    public string? ErrorMessage { get; init; }
}
