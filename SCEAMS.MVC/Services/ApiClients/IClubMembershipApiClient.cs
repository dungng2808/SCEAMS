using SCEAMS.MVC.Models.Api;

namespace SCEAMS.MVC.Services.ApiClients;

public interface IClubMembershipApiClient
{
    Task<PendingMembershipsApiResult> GetPendingMembershipsAsync(
        int clubId,
        string? search,
        int page = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default);

    Task<PendingMembershipsApiResult> GetActiveMembershipsAsync(
        int clubId,
        string? search,
        int page = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default);

    Task<DecideClubMembershipApiResult> DecideMembershipAsync(
        int clubId,
        int userId,
        DecideClubMembershipApiRequest request,
        CancellationToken cancellationToken = default);

    Task<RemoveClubMembershipApiResult> RemoveMembershipAsync(
        int clubId,
        int userId,
        RemoveClubMembershipApiRequest request,
        CancellationToken cancellationToken = default);
}
