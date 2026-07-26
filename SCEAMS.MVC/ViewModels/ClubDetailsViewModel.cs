namespace SCEAMS.MVC.ViewModels;

public sealed class ClubDetailsViewModel
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
    public string? ReviewedAtFormatted { get; init; }
    public string? RejectionReason { get; init; }
    public string? DissolvedAtFormatted { get; init; }
    public string Initials { get; init; } = string.Empty;

    // Flags for actions according to role & ownership
    public bool CanEdit { get; init; }
    public bool CanApproveOrReject { get; init; }
    public bool CanDissolve { get; init; }
    public bool CanJoin { get; init; }

    public bool IsNotFound { get; init; }
    public bool IsForbidden { get; init; }
    public string? ErrorMessage { get; init; }
}
