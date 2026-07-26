namespace SCEAMS.MVC.Models.Api;

public sealed class ClubListApiResult
{
    public bool IsSuccess { get; init; }
    public bool IsUnauthorized { get; init; }
    public bool IsForbidden { get; init; }
    public string? ErrorMessage { get; init; }
    public IReadOnlyList<ClubApiResponse> Clubs { get; init; } = [];
    public int TotalItems { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 10;
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalItems / PageSize) : 0;
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;
}
