using SCEAMS.Domain.Enums;

namespace SCEAMS.Application.DTOs;

public sealed class ClubMembershipResponseDto
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string StudentEmail { get; set; } = string.Empty;
    public int ClubId { get; set; }
    public string ClubName { get; set; } = string.Empty;
    public string RoleInClub { get; set; } = "Member";
    public DateTime JoinDate { get; set; }
    public ClubMembershipStatus Status { get; set; }
    public int? DecidedByUserId { get; set; }
    public DateTime? DecisionAt { get; set; }
    public string? RemovalReason { get; set; }
}
