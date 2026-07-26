using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using SCEAMS.MVC.Models.Api;

namespace SCEAMS.MVC.Services.ApiClients;

public sealed class UserApiClient : IUserApiClient
{
    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;

    public UserApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<UserListApiResult> GetUsersAsync(
        UserListApiQuery query,
        CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.GetAsync(
            BuildUserListUri(query),
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.OK)
        {
            var users = await response.Content
                .ReadFromJsonAsync<PagedUsersApiResponse>(
                    cancellationToken: cancellationToken);
            var isValid = users?.Items is not null;

            return new UserListApiResult
            {
                IsSuccess = isValid,
                Users = isValid ? users : null,
                ErrorMessage = !isValid
                    ? "API trả về danh sách người dùng không hợp lệ."
                    : null
            };
        }

        return response.StatusCode switch
        {
            HttpStatusCode.Unauthorized =>
                new UserListApiResult
                {
                    IsUnauthorized = true,
                    ErrorMessage =
                        "Phiên đăng nhập đã hết hạn hoặc không hợp lệ."
                },
            HttpStatusCode.Forbidden =>
                new UserListApiResult
                {
                    IsForbidden = true,
                    ErrorMessage =
                        "Bạn không có quyền xem danh sách người dùng."
                },
            HttpStatusCode.BadRequest =>
                new UserListApiResult
                {
                    ErrorMessage =
                        "Bộ lọc hoặc thông tin phân trang không hợp lệ."
                },
            _ => new UserListApiResult
            {
                ErrorMessage =
                    "Không thể tải danh sách người dùng vào lúc này."
            }
        };
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

    public async Task<UpdateCurrentUserProfileApiResult>
        UpdateCurrentUserAsync(
            UpdateCurrentUserProfileApiRequest request,
            CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.PutAsJsonAsync(
            "api/users/me",
            request,
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.OK)
        {
            var profile = await response.Content
                .ReadFromJsonAsync<CurrentUserProfileApiResponse>(
                    cancellationToken: cancellationToken);

            return new UpdateCurrentUserProfileApiResult
            {
                IsSuccess = profile is not null,
                Profile = profile,
                ErrorMessage = profile is null
                    ? "API trả về dữ liệu hồ sơ không hợp lệ."
                    : null
            };
        }

        var content = await response.Content.ReadAsStringAsync(
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            var validationProblem =
                DeserializeOrDefault<ValidationProblemApiResponse>(
                    content);

            if (validationProblem?.Errors is { Count: > 0 })
            {
                return new UpdateCurrentUserProfileApiResult
                {
                    FieldErrors = validationProblem.Errors
                };
            }
        }

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            return new UpdateCurrentUserProfileApiResult
            {
                IsUnauthorized = true,
                ErrorMessage =
                    "Phiên đăng nhập đã hết hạn hoặc không hợp lệ."
            };
        }

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return new UpdateCurrentUserProfileApiResult
            {
                IsNotFound = true,
                ErrorMessage =
                    "Tài khoản không còn tồn tại trong hệ thống."
            };
        }

        var apiError = DeserializeOrDefault<ApiErrorResponse>(
            content);

        return new UpdateCurrentUserProfileApiResult
        {
            ErrorMessage = apiError?.Message ??
                "Không thể cập nhật hồ sơ vào lúc này."
        };
    }

    public async Task<ChangeCurrentUserPasswordApiResult>
        ChangeCurrentUserPasswordAsync(
            ChangeCurrentUserPasswordApiRequest request,
            CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.PutAsJsonAsync(
            "api/users/me/password",
            request,
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.NoContent)
        {
            return new ChangeCurrentUserPasswordApiResult
            {
                IsSuccess = true
            };
        }

        var content = await response.Content.ReadAsStringAsync(
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            var validationProblem =
                DeserializeOrDefault<ValidationProblemApiResponse>(
                    content);

            if (validationProblem?.Errors is { Count: > 0 })
            {
                return new ChangeCurrentUserPasswordApiResult
                {
                    FieldErrors = validationProblem.Errors
                };
            }

            var badRequest = DeserializeOrDefault<ApiErrorResponse>(
                content);

            return new ChangeCurrentUserPasswordApiResult
            {
                ErrorMessage = badRequest?.Message ==
                    "Current password is incorrect."
                    ? "Mật khẩu hiện tại không chính xác."
                    : badRequest?.Message ??
                        "Thông tin đổi mật khẩu không hợp lệ."
            };
        }

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            return new ChangeCurrentUserPasswordApiResult
            {
                IsUnauthorized = true,
                ErrorMessage =
                    "Phiên đăng nhập đã hết hạn hoặc không hợp lệ."
            };
        }

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return new ChangeCurrentUserPasswordApiResult
            {
                IsNotFound = true,
                ErrorMessage =
                    "Tài khoản không còn tồn tại trong hệ thống."
            };
        }

        return new ChangeCurrentUserPasswordApiResult
        {
            ErrorMessage =
                "Không thể đổi mật khẩu vào lúc này."
        };
    }

    private static string BuildUserListUri(
        UserListApiQuery query)
    {
        var parameters = new List<string>
        {
            $"page={query.Page.ToString(CultureInfo.InvariantCulture)}",
            $"pageSize={query.PageSize.ToString(CultureInfo.InvariantCulture)}"
        };

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            parameters.Add(
                $"search={Uri.EscapeDataString(query.Search)}");
        }

        if (!string.IsNullOrWhiteSpace(query.Role))
        {
            parameters.Add(
                $"role={Uri.EscapeDataString(query.Role)}");
        }

        if (query.IsActive.HasValue)
        {
            parameters.Add(
                $"isActive={query.IsActive.Value.ToString().ToLowerInvariant()}");
        }

        return $"api/users?{string.Join('&', parameters)}";
    }

    private sealed record ApiErrorResponse(string? Message);

    private sealed record ValidationProblemApiResponse(
        Dictionary<string, string[]> Errors);

    private static T? DeserializeOrDefault<T>(string content)
        where T : class
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<T>(
                content,
                JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
