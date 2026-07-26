using SCEAMS.MVC.Models.Api;

namespace SCEAMS.MVC.Services.ApiClients;

public interface IClubCategoryApiClient
{
    Task<CreateClubCategoryApiResult> CreateClubCategoryAsync(
        CreateClubCategoryApiRequest request,
        CancellationToken cancellationToken = default);

    Task<ClubCategoryListApiResult> GetClubCategoriesAsync(
        CancellationToken cancellationToken = default);
}
