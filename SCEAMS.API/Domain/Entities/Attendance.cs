namespace SCEAMS.Domain.Entities;

public class Attendance
{
    public int Id { get; set; }
    public int RegistrationId { get; set; }
    public DateTime CheckInTime { get; set; }
    public int CheckedInByUserId { get; set; }

    public Registration Registration { get; set; } = null!;
    public User CheckedInByUser { get; set; } = null!;
}
