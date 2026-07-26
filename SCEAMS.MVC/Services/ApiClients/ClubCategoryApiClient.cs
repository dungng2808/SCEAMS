using System.Net;
using System.Net.Http.Json;
using SCEAMS.MVC.Models.Api;

namespace SCEAMS.MVC.Services.ApiClients;

public sealed class ClubCategoryApiClient
    : IClubCategoryApiClient
{
    private readonly HttpClient _httpClient;

    public ClubCategoryApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<ClubCategoryListApiResult>
        GetClubCategoriesAsync(
            CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.GetAsync(
            "api/club-categories",
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.OK)
        {
            var categories = await response.Content
                .ReadFromJsonAsync<List<ClubCategoryApiResponse>>(
                    cancellationToken: cancellationToken);

            return new ClubCategoryListApiResult
            {
                IsSuccess = categories is not null,
                Categories = categories ?? [],
                ErrorMessage = categories is null
                    ? "API trả về danh sách danh mục không hợp lệ."
                    : null
            };
        }

        return new ClubCategoryListApiResult
        {
            ErrorMessage =
                "Không thể tải danh mục câu lạc bộ vào lúc này."
        };
    }
}
