namespace SCEAMS.MVC.Models.Api;

public sealed record CreateClubCategoryApiRequest(
    string Name,
    string? Description);
