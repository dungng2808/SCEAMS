namespace SCEAMS.MVC.Models.Api;

public sealed record UserListApiQuery(
    string? Search,
    string? Role,
    bool? IsActive,
    int Page,
    int PageSize);
