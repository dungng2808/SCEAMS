namespace SCEAMS.MVC.Models.Api;

public sealed record ClubListApiQuery(
    int? CategoryId = null,
    string? Search = null,
    string? Status = null,
    string? OrderBy = null,
    int Page = 1,
    int PageSize = 10);
