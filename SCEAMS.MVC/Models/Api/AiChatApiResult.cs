namespace SCEAMS.MVC.Models.Api;

public sealed class AiChatApiResult
{
    public bool IsSuccess { get; init; }
    public int StatusCode { get; init; }
    public bool IsUnauthorized => StatusCode == 401;
    public bool IsForbidden => StatusCode == 403;
    public bool IsProviderUnavailable => StatusCode == 503;
    public string Answer { get; init; } = string.Empty;
    public IReadOnlyList<EventFaqEventApiResponse> Events { get; init; } = [];
    public string? ErrorMessage { get; init; }
}
