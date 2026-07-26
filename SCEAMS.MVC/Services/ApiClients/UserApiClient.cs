using System.Net;
using System.Net.Http.Json;
using SCEAMS.MVC.Models.Api;

namespace SCEAMS.MVC.Services.ApiClients;

public sealed class UserApiClient : IUserApiClient
{
    private readonly HttpClient _httpClient;

    public UserApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<CurrentUserProfileApiResult> GetCurrentUserAsync(
        CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.GetAsync(
            "api/users/me",
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.OK)
        {
            var profile = await response.Content
                .ReadFromJsonAsync<CurrentUserProfileApiResponse>(
                    cancellationToken: cancellationToken);

            return new CurrentUserProfileApiResult
            {
                IsSuccess = profile is not null,
                Profile = profile,
                ErrorMessage = profile is null
                    ? "API trả về dữ liệu hồ sơ không hợp lệ."
                    : null
            };
        }

        return response.StatusCode switch
        {
            HttpStatusCode.Unauthorized =>
                new CurrentUserProfileApiResult
                {
                    IsUnauthorized = true,
                    ErrorMessage =
                        "Phiên đăng nhập đã hết hạn hoặc không hợp lệ."
                },
            HttpStatusCode.NotFound =>
                new CurrentUserProfileApiResult
                {
                    IsNotFound = true,
                    ErrorMessage =
                        "Tài khoản không còn tồn tại trong hệ thống."
                },
            _ => new CurrentUserProfileApiResult
            {
                ErrorMessage =
                    "Không thể tải hồ sơ vào lúc này."
            }
        };
    }
}
