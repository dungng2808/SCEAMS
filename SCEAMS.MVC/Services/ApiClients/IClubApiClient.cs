using SCEAMS.MVC.Models.Api;

namespace SCEAMS.MVC.Services.ApiClients;

public interface IClubApiClient
{
    Task<ClubListApiResult> GetClubsAsync(
        ClubListApiQuery query,
        CancellationToken cancellationToken = default);

    Task<ClubDetailApiResult> GetClubByIdAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<CreateClubApiResult> CreateClubAsync(
        CreateClubApiRequest request,
        CancellationToken cancellationToken = default);

    Task<ApproveClubApiResult> ApproveClubAsync(
        int id,
        CancellationToken cancellationToken = default);
}
