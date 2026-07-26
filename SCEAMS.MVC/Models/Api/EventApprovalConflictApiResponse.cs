namespace SCEAMS.MVC.Models.Api;

public sealed record EventApprovalConflictApiResponse(
    int EventId,
    string Title,
    string VenueName,
    string Status,
    DateTime StartTime,
    DateTime EndTime);
