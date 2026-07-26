namespace SCEAMS.MVC.Models.Api;

public sealed record UserActiveStatusApiResponse(
    int Id,
    string FullName,
    string Email,
    string Role,
    bool IsActive);
