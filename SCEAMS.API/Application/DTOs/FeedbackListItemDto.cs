namespace SCEAMS.Application.DTOs;

public sealed class FeedbackListItemDto
{
    public int Id { get; init; }
    public int Rating { get; init; }
    public string? Comment { get; init; }
    public DateTime CreatedAt { get; init; }
}
