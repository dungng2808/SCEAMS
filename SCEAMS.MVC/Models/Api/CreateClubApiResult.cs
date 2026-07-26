namespace SCEAMS.MVC.Models.Api;

public sealed class CreateClubApiResult
{
    public bool IsSuccess { get; init; }
    public bool IsUnauthorized { get; init; }
    public bool IsForbidden { get; init; }
    public bool IsConflict { get; init; }
    public string? ErrorMessage { get; init; }
    public IDictionary<string, string[]>? ValidationErrors { get; init; }
    public ClubDetailApiResponse? Club { get; init; }
}
