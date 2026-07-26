using SCEAMS.Domain.Enums;

namespace SCEAMS.Domain.Entities;

public class User
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public string? StudentCode { get; set; }
    public string? PhoneNumber { get; set; }
    public bool IsActive { get; set; } = true;
    public string? RefreshTokenHash { get; set; }
    public DateTime? RefreshTokenExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }

    public ICollection<Club> CreatedClubs { get; set; } = [];
    public ICollection<Club> ReviewedClubs { get; set; } = [];
    public ICollection<ClubMembership> ClubMemberships { get; set; } = [];
    public ICollection<ClubMembership> MembershipDecisions { get; set; } = [];
    public ICollection<Event> CreatedEvents { get; set; } = [];
    public ICollection<Event> ApprovedEvents { get; set; } = [];
    public ICollection<Registration> Registrations { get; set; } = [];
    public ICollection<Attendance> CheckedInAttendances { get; set; } = [];
    public ICollection<Feedback> Feedbacks { get; set; } = [];
    public ICollection<ChatLog> ChatLogs { get; set; } = [];
}
