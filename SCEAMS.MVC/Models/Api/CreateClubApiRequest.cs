namespace SCEAMS.MVC.Models.Api;

public sealed class CreateClubApiRequest
{
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public int CategoryId { get; init; }
}
