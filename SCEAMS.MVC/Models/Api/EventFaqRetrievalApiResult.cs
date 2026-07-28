namespace SCEAMS.MVC.Models.Api;

public sealed class EventFaqRetrievalApiResult
{
    public bool IsSuccess { get; init; }
    public int StatusCode { get; init; }
    public bool IsUnauthorized => StatusCode == 401;
    public bool IsForbidden => StatusCode == 403;
    public bool IsBadRequest => StatusCode == 400;
    public IReadOnlyList<EventFaqEventApiResponse> Events { get; init; } = [];
    public string? ErrorMessage { get; init; }
}
