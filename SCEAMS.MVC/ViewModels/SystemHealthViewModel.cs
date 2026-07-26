namespace SCEAMS.MVC.ViewModels;

public sealed class SystemHealthViewModel
{
    public ApiHealthStatusViewModel Api { get; init; } = new();
    public DatabaseHealthStatusViewModel Database { get; init; } = new();
    public DateTimeOffset CheckedAt { get; init; }
}

public sealed class ApiHealthStatusViewModel
{
    public bool IsOnline { get; init; }
    public string Service { get; init; } = "SCEAMS.API";
    public string Version { get; init; } = "Unknown";
    public string Status { get; init; } = "Offline";
    public string? ErrorMessage { get; init; }
}

public sealed class DatabaseHealthStatusViewModel
{
    public bool IsOnline { get; init; }
    public string DatabaseName { get; init; } = "SCEAMS";
    public string Status { get; init; } = "Offline";
    public bool DemoSeedReady { get; init; }
    public int DemoAccountCount { get; init; }
    public string? ErrorMessage { get; init; }
}
