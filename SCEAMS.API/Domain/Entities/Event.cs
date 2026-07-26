using SCEAMS.Domain.Enums;

namespace SCEAMS.Domain.Entities;

public class Event
{
    public int Id { get; set; }
    public int ClubId { get; set; }
    public int VenueId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public DateTime RegistrationDeadline { get; set; }
    public int Capacity { get; set; }
    public EventStatus Status { get; set; } = EventStatus.Draft;
    public int CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int? ApprovedByUserId { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? RejectionReason { get; set; }
    public string? CancellationReason { get; set; }
    public byte[] RowVersion { get; set; } = [];

    public Club Club { get; set; } = null!;
    public Venue Venue { get; set; } = null!;
    public User CreatedByUser { get; set; } = null!;
    public User? ApprovedByUser { get; set; }
    public ICollection<Registration> Registrations { get; set; } = [];
    public ICollection<Feedback> Feedbacks { get; set; } = [];
}
