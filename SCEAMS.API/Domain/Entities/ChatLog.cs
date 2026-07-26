namespace SCEAMS.Domain.Entities;

public class ChatLog
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public string Question { get; set; } = string.Empty;
    public string RelatedEventIds { get; set; } = "[]";
    public string AnswerText { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    public User Student { get; set; } = null!;
}
