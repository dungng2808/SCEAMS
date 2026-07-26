namespace SCEAMS.MVC.Models.Api;

public sealed record UpdateVenueApiRequest(
    string Name,
    string Location,
    int Capacity);
