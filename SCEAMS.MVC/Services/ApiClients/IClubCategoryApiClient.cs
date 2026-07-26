using SCEAMS.MVC.Models.Api;

namespace SCEAMS.MVC.Services.ApiClients;

public interface IClubCategoryApiClient
{
    Task<CreateClubCategoryApiResult> CreateClubCategoryAsync(
        CreateClubCategoryApiRequest request,
        CancellationToken cancellationToken = default);

    Task<ClubCategoryListApiResult> GetClubCategoriesAsync(
        CancellationToken cancellationToken = default);

    Task<ClubCategoryApiResult> GetClubCategoryAsync(
        int categoryId,
        CancellationToken cancellationToken = default);

    Task<UpdateClubCategoryApiResult> UpdateClubCategoryAsync(
        int categoryId,
        UpdateClubCategoryApiRequest request,
        CancellationToken cancellationToken = default);

    Task<DeleteClubCategoryApiResult> DeleteClubCategoryAsync(
        int categoryId,
        CancellationToken cancellationToken = default);
}

