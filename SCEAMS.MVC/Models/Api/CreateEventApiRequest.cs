namespace SCEAMS.MVC.Models.Api;

public sealed record CreateEventApiRequest(
    string Title,
    string? Description,
    int ClubId,
    int VenueId,
    DateTime StartTime,
    DateTime EndTime,
    DateTime RegistrationDeadline,
    int Capacity);
