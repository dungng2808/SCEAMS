using SCEAMS.Domain.Enums;

namespace SCEAMS.Domain.Entities;

public class Registration
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public int EventId { get; set; }
    public RegistrationStatus Status { get; set; } = RegistrationStatus.Pending;
    public DateTime RegisteredAt { get; set; }
    public DateTime? CancelledAt { get; set; }

    public User Student { get; set; } = null!;
    public Event Event { get; set; } = null!;
    public Attendance? Attendance { get; set; }
}
