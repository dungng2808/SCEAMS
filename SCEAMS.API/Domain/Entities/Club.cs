using SCEAMS.Domain.Enums;

namespace SCEAMS.Domain.Entities;

public class Club
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int CategoryId { get; set; }
    public ClubStatus Status { get; set; } = ClubStatus.PendingApproval;
    public int CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public int? ReviewedByUserId { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? RejectionReason { get; set; }
    public DateTime? DissolvedAt { get; set; }

    public ClubCategory Category { get; set; } = null!;
    public User CreatedByUser { get; set; } = null!;
    public User? ReviewedByUser { get; set; }
    public ICollection<ClubMembership> Memberships { get; set; } = [];
    public ICollection<Event> Events { get; set; } = [];
}
