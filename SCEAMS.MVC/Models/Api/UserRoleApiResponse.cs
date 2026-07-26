namespace SCEAMS.MVC.Models.Api;

public sealed record UserRoleApiResponse(
    int Id,
    string FullName,
    string Email,
    string Role,
    bool IsActive);
