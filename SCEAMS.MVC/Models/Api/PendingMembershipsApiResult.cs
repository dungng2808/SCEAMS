namespace SCEAMS.MVC.Models.Api;

public sealed class PendingMembershipsApiResult
{
    public bool IsSuccess { get; init; }
    public bool IsNotFound { get; init; }
    public bool IsUnauthorized { get; init; }
    public bool IsForbidden { get; init; }
    public string? ErrorMessage { get; init; }
    public IReadOnlyList<ClubMembershipApiResponse> Items { get; init; } = [];
    public int TotalItems { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 10;
}
