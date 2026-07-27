namespace SCEAMS.Infrastructure.GrpcClients;

public sealed class NotificationGrpcOptions
{
    public const string SectionName = "NotificationGrpc";

    public string Address { get; set; } = "https://localhost:7001";
    public int TimeoutSeconds { get; set; } = 3;
    public int MaxRetries { get; set; } = 1;
}
