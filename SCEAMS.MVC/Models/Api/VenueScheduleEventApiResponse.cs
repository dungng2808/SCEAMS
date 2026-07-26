namespace SCEAMS.MVC.Models.Api;

public sealed record VenueScheduleEventApiResponse(
    int EventId,
    string Title,
    string Status,
    DateTime StartTime,
    DateTime EndTime);
