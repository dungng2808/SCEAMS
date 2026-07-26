namespace SCEAMS.MVC.ViewModels;

public sealed class ClubsViewModel
{
    public int? CategoryId { get; init; }
    public string? Search { get; init; }
    public string? Status { get; init; }
    public string SortBy { get; init; } = "name_asc";
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 10;
    public int TotalItems { get; init; }
    public int TotalPages { get; init; }
    public bool HasPreviousPage { get; init; }
    public bool HasNextPage { get; init; }

    public bool CanManage { get; init; }
    public bool CanCreateClub { get; init; }
    public bool IsForbidden { get; init; }
    public string? ErrorMessage { get; init; }

    public IReadOnlyList<ClubCategorySelectItemViewModel> Categories { get; init; } = [];
    public IReadOnlyList<ClubListItemViewModel> Clubs { get; init; } = [];
}

public sealed class ClubCategorySelectItemViewModel
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
}

public sealed class ClubListItemViewModel
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public int CategoryId { get; init; }
    public string CategoryName { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string StatusLabel { get; init; } = string.Empty;
    public string StatusBadgeClass { get; init; } = string.Empty;
    public int CreatedByUserId { get; init; }
    public string CreatedByUserName { get; init; } = string.Empty;
    public int ActiveMemberCount { get; init; }
    public string CreatedAtFormatted { get; init; } = string.Empty;
    public string Initials { get; init; } = string.Empty;
    public string Theme { get; init; } = string.Empty;
}
