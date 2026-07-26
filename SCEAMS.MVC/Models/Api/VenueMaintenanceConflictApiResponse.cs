namespace SCEAMS.MVC.Models.Api;

public sealed record VenueMaintenanceConflictApiResponse(
    int EventId,
    string Title,
    string Status,
    DateTime StartTime,
    DateTime EndTime);
