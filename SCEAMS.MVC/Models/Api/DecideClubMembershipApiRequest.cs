namespace SCEAMS.MVC.Models.Api;

public sealed record DecideClubMembershipApiRequest(
    bool Approve,
    string? RejectionReason = null);
