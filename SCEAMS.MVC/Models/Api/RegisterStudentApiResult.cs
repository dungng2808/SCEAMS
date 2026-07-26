namespace SCEAMS.MVC.Models.Api;

public sealed class RegisterStudentApiResult
{
    public bool IsSuccess { get; init; }
    public RegisteredStudentApiResponse? Student { get; init; }
    public IReadOnlyDictionary<string, string[]> FieldErrors { get; init; } =
        new Dictionary<string, string[]>();
    public string? ErrorMessage { get; init; }
}
