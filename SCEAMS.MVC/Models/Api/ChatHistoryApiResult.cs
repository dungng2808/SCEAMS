namespace SCEAMS.MVC.Models.Api;

public sealed class ChatHistoryApiResult
{
    public bool IsSuccess { get; init; }
    public int StatusCode { get; init; }
    public bool IsUnauthorized => StatusCode == 401;
    public bool IsForbidden => StatusCode == 403;
    public ChatHistoryPageApiResponse? Page { get; init; }
    public string? ErrorMessage { get; init; }
}
