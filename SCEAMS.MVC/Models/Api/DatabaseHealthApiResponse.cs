namespace SCEAMS.MVC.Models.Api;

public sealed record DatabaseHealthApiResponse(
    string Database,
    string Status,
    bool CanConnect,
    bool DemoSeedReady,
    int DemoAccountCount);
