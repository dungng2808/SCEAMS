namespace SCEAMS.Domain.Entities;

public class Feedback
{
    public int Id { get; set; }
    public int EventId { get; set; }
    public int StudentId { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; }

    public Event Event { get; set; } = null!;
    public User Student { get; set; } = null!;
}
