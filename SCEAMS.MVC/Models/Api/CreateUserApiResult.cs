namespace SCEAMS.MVC.Models.Api;

public sealed class CreateUserApiResult
{
    public bool IsSuccess { get; init; }
    public bool IsUnauthorized { get; init; }
    public bool IsForbidden { get; init; }
    public CreatedUserApiResponse? User { get; init; }
    public IReadOnlyDictionary<string, string[]> FieldErrors { get; init; } =
        new Dictionary<string, string[]>();
    public string? ErrorMessage { get; init; }
}
