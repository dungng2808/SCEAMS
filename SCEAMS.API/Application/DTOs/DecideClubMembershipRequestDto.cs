namespace SCEAMS.Application.DTOs;

public sealed class DecideClubMembershipRequestDto
{
    public bool Approve { get; set; }
    public string? RejectionReason { get; set; }
}
