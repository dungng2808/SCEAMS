namespace SCEAMS.Application.DTOs;

public sealed record PagedUsersResponseDto(
    IReadOnlyList<UserListItemResponseDto> Items,
    int Page,
    int PageSize,
    int TotalItems,
    int TotalPages,
    bool HasPreviousPage,
    bool HasNextPage);
