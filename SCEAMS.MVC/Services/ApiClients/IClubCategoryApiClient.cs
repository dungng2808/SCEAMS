using SCEAMS.MVC.Models.Api;

namespace SCEAMS.MVC.Services.ApiClients;

public interface IClubCategoryApiClient
{
    Task<ClubCategoryListApiResult> GetClubCategoriesAsync(
        CancellationToken cancellationToken = default);
}
