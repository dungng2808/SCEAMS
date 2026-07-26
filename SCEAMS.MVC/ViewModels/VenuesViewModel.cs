namespace SCEAMS.MVC.ViewModels;

public sealed class VenuesViewModel
{
    public string? Search { get; init; }
    public bool? Maintenance { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 10;
    public int TotalItems { get; init; }
    public int TotalPages { get; init; }
    public bool HasPreviousPage { get; init; }
    public bool HasNextPage { get; init; }
    public bool CanManage { get; init; }
    public string? ErrorMessage { get; init; }
    public IReadOnlyList<VenueListItemViewModel> Venues { get; init; } = [];
}

public sealed class VenueListItemViewModel
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Location { get; init; } = string.Empty;
    public int Capacity { get; init; }
    public bool IsUnderMaintenance { get; init; }
    public string MaintenanceLabel => IsUnderMaintenance ? "Đang bảo trì" : "Sẵn sàng";
    public string MaintenanceBadgeClass => IsUnderMaintenance
        ? "status-badge--warning"
        : "status-badge--success";
}
