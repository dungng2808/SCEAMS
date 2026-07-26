namespace SCEAMS.MVC.Models.Api;

public sealed record UserListItemApiResponse(
    int Id,
    string FullName,
    string Email,
    string? StudentCode,
    string? PhoneNumber,
    string Role,
    bool IsActive,
    DateTime CreatedAt);
