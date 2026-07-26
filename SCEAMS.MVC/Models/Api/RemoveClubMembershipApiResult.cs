namespace SCEAMS.MVC.Models.Api;

public sealed class RemoveClubMembershipApiResult
{
    public bool IsSuccess { get; init; }
    public bool IsNotFound { get; init; }
    public bool IsUnauthorized { get; init; }
    public bool IsForbidden { get; init; }
    public bool IsConflict { get; init; }
    public bool IsValidationError { get; init; }
    public string? ErrorMessage { get; init; }
    public ClubMembershipApiResponse? Membership { get; init; }
}
