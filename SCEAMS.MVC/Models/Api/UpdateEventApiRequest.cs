namespace SCEAMS.MVC.Models.Api;

public sealed record UpdateEventApiRequest(
    string Title,
    string? Description,
    int VenueId,
    DateTime StartTime,
    DateTime EndTime,
    DateTime RegistrationDeadline,
    int Capacity);
