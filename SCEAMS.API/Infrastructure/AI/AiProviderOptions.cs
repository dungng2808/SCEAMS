namespace SCEAMS.Infrastructure.AI;

public sealed class AiProviderOptions
{
    public const string SectionName = "AI";

    public bool Enabled { get; init; }
    public string Endpoint { get; init; } = string.Empty;
    public string ApiKey { get; init; } = string.Empty;
    public string Model { get; init; } = string.Empty;
    public int TimeoutSeconds { get; init; } = 15;
}
