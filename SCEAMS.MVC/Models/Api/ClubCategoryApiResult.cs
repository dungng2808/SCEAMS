namespace SCEAMS.MVC.Models.Api;

public sealed class ClubCategoryApiResult
{
    public bool IsSuccess { get; init; }
    public bool IsUnauthorized { get; init; }
    public bool IsNotFound { get; init; }
    public ClubCategoryApiResponse? Category { get; init; }
    public string? ErrorMessage { get; init; }
}
