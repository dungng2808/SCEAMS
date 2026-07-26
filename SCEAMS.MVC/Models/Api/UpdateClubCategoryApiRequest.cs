namespace SCEAMS.MVC.Models.Api;

public sealed record UpdateClubCategoryApiRequest(
    string Name,
    string? Description);
