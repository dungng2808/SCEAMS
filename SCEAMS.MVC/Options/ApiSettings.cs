namespace SCEAMS.MVC.Options;

public sealed class ApiSettings
{
    public const string SectionName = "ApiSettings";

    public string BaseUrl { get; init; } = string.Empty;
    public int TimeoutSeconds { get; init; } = 5;
}
