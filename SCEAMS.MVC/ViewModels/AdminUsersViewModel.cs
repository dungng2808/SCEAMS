namespace SCEAMS.MVC.ViewModels;

public sealed class AdminUsersViewModel
{
    public string? Search { get; init; }
    public string? Role { get; init; }
    public bool? IsActive { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 10;
    public int TotalItems { get; init; }
    public int TotalPages { get; init; }
    public bool HasPreviousPage { get; init; }
    public bool HasNextPage { get; init; }
    public bool IsForbidden { get; init; }
    public string? ErrorMessage { get; init; }
    public IReadOnlyList<AdminUserListItemViewModel> Users { get; init; } = [];

    public bool HasFilters =>
        !string.IsNullOrWhiteSpace(Search) ||
        !string.IsNullOrWhiteSpace(Role) ||
        IsActive.HasValue;

    public int FirstItemNumber =>
        Users.Count == 0
            ? 0
            : (Page - 1) * PageSize + 1;

    public int LastItemNumber =>
        Users.Count == 0
            ? 0
            : Math.Min(
                FirstItemNumber + Users.Count - 1,
                TotalItems);
}

public sealed class AdminUserListItemViewModel
{
    public int Id { get; init; }
    public string Initials { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string? StudentCode { get; init; }
    public string? PhoneNumber { get; init; }
    public string Role { get; init; } = string.Empty;
    public string RoleLabel { get; init; } = string.Empty;
    public string RoleCssClass { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public DateTime CreatedAtUtc { get; init; }
    public DateTimeOffset CreatedAtLocal { get; init; }
}
