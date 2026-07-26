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

    Task<RejectClubApiResult> RejectClubAsync(
        int id,
        RejectClubApiRequest request,
        CancellationToken cancellationToken = default);

    Task<UpdateClubApiResult> UpdateClubAsync(
        int id,
        UpdateClubApiRequest request,
        CancellationToken cancellationToken = default);

    Task<DissolveClubApiResult> DissolveClubAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<RequestJoinClubApiResult> RequestJoinClubAsync(
        int clubId,
        CancellationToken cancellationToken = default);
}
