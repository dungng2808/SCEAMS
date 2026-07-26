namespace SCEAMS.MVC.Models.Api;

public sealed class RequestJoinClubApiResult
{
    public bool IsSuccess { get; init; }
    public bool IsNotFound { get; init; }
    public bool IsConflict { get; init; }
    public bool IsUnauthorized { get; init; }
    public bool IsForbidden { get; init; }
    public string? ErrorMessage { get; init; }
    public ClubMembershipApiResponse? Membership { get; init; }
}

public sealed class ClubMembershipApiResponse
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string StudentEmail { get; set; } = string.Empty;
    public int ClubId { get; set; }
    public string ClubName { get; set; } = string.Empty;
    public string RoleInClub { get; set; } = "Member";
    public DateTime JoinDate { get; set; }
    public string Status { get; set; } = string.Empty;
}
