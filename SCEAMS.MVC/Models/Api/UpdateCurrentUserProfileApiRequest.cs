namespace SCEAMS.MVC.Models.Api;

public sealed record UpdateCurrentUserProfileApiRequest(
    string FullName,
    string? PhoneNumber);
