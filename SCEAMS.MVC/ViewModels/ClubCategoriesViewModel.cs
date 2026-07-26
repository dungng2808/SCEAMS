namespace SCEAMS.MVC.ViewModels;

public sealed class ClubCategoriesViewModel
{
    public bool CanManage { get; init; }
    public string? ErrorMessage { get; init; }
    public IReadOnlyList<ClubCategoryListItemViewModel> Categories { get; init; }
        = [];
}

public sealed class ClubCategoryListItemViewModel
{
    public int Id { get; init; }
    public int SequenceNumber { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string Initials { get; init; } = string.Empty;
    public string Theme { get; init; } = string.Empty;
}
