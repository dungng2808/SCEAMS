namespace SCEAMS.MVC.Models.Api;

public sealed class LoginApiResult
{
    public bool IsSuccess { get; init; }
    public LoginApiResponse? Response { get; init; }
    public IReadOnlyDictionary<string, string[]> FieldErrors { get; init; } =
        new Dictionary<string, string[]>();
    public string? ErrorMessage { get; init; }
}
