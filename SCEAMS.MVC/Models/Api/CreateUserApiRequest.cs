namespace SCEAMS.MVC.Models.Api;

public sealed record CreateUserApiRequest(
    string FullName,
    string Email,
    string? StudentCode,
    string? PhoneNumber,
    string Role,
    bool IsActive,
    string Password,
    string ConfirmPassword);
