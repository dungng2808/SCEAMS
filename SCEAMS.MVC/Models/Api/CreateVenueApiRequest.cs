namespace SCEAMS.MVC.Models.Api;

public sealed record CreateVenueApiRequest(
    string Name,
    string Location,
    int Capacity);
