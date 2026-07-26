namespace SCEAMS.MVC.Models.Api;

public sealed record UpdateUserProfileApiRequest(
    string FullName,
    string Email,
    string? StudentCode,
    string? PhoneNumber);
