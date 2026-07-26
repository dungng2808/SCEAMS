using SCEAMS.Domain.Enums;

namespace SCEAMS.Domain.Entities;

public class ClubMembership
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public int ClubId { get; set; }
    public string RoleInClub { get; set; } = "Member";
    public DateTime JoinDate { get; set; }
    public ClubMembershipStatus Status { get; set; } = ClubMembershipStatus.Pending;
    public int? DecidedByUserId { get; set; }
    public DateTime? DecisionAt { get; set; }
    public string? RemovalReason { get; set; }

    public User Student { get; set; } = null!;
    public Club Club { get; set; } = null!;
    public User? DecidedByUser { get; set; }
}
