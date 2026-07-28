namespace SCEAMS.Application.Common;

public sealed record AiProviderResult(
    bool IsSuccess,
    string? Answer,
    string? ErrorMessage)
{
    public static AiProviderResult Success(string answer) => new(
        true,
        answer,
        null);

    public static AiProviderResult Unavailable(string message) => new(
        false,
        null,
        message);
}
