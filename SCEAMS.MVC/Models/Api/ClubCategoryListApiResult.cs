namespace SCEAMS.MVC.Models.Api;

public sealed class ClubCategoryListApiResult
{
    public bool IsSuccess { get; init; }
    public IReadOnlyList<ClubCategoryApiResponse> Categories { get; init; } = [];
    public string? ErrorMessage { get; init; }
}
