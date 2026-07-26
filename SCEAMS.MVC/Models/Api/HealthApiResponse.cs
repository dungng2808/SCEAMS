namespace SCEAMS.MVC.Models.Api;

public sealed record HealthApiResponse(
    string Service,
    string Version,
    string Status);
