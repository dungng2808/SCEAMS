using SCEAMS.MVC.Models.Api;

namespace SCEAMS.MVC.Services.ApiClients;

public interface IClubApiClient
{
    Task<ClubListApiResult> GetClubsAsync(
        ClubListApiQuery query,
        CancellationToken cancellationToken = default);
}
