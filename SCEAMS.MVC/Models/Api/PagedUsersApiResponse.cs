namespace SCEAMS.MVC.Models.Api;

public sealed record PagedUsersApiResponse(
    IReadOnlyList<UserListItemApiResponse> Items,
    int Page,
    int PageSize,
    int TotalItems,
    int TotalPages,
    bool HasPreviousPage,
    bool HasNextPage);
