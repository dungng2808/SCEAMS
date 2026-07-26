namespace SCEAMS.MVC.ViewModels;

public sealed class ClubMembersViewModel
{
    public int ClubId { get; set; }
    public string ClubName { get; set; } = string.Empty;
    public string ActiveTab { get; set; } = "pending";
    public string? Search { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalItems { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalItems / Math.Max(PageSize, 1));
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;
    public bool IsForbidden { get; set; }
    public bool IsNotFound { get; set; }
    public string? ErrorMessage { get; set; }
    public List<ClubMembershipItemViewModel> Members { get; set; } = [];
}

public sealed class ClubMembershipItemViewModel
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string StudentEmail { get; set; } = string.Empty;
    public string RoleInClub { get; set; } = "Member";
    public string JoinDateFormatted { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string StatusLabel { get; set; } = string.Empty;
    public string StatusBadgeClass { get; set; } = string.Empty;
    public string Initials { get; set; } = string.Empty;
}
