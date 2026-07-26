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
}
