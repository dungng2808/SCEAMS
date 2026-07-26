namespace SCEAMS.MVC.Models.Api;

public sealed record ClubCategoryApiResponse(
    int Id,
    string Name,
    string? Description);
