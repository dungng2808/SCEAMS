namespace SCEAMS.MVC.Models.Api;

public sealed class ApproveEventApiResult
{
    public bool IsSuccess { get; init; }
    public bool IsUnauthorized { get; init; }
    public bool IsForbidden { get; init; }
    public bool IsNotFound { get; init; }
    public bool IsConflict { get; init; }
    public EventDetailApiResponse? Event { get; init; }
    public IReadOnlyList<EventApprovalConflictApiResponse> Conflicts { get; init; } = [];
    public string? ErrorMessage { get; init; }
}
