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

    public async Task<CreateUserApiResult> CreateUserAsync(
        CreateUserApiRequest request,
        CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.PostAsJsonAsync(
            "api/users",
            request,
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.Created)
        {
            var user = await response.Content
                .ReadFromJsonAsync<CreatedUserApiResponse>(
                    cancellationToken: cancellationToken);

            return new CreateUserApiResult
            {
                IsSuccess = user is not null,
                User = user,
                ErrorMessage = user is null
                    ? "API trả về tài khoản vừa tạo không hợp lệ."
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
                return new CreateUserApiResult
                {
                    FieldErrors = validationProblem.Errors
                };
            }

            var badRequest = DeserializeOrDefault<ApiErrorResponse>(
                content);

            return CreateBadRequestResult(badRequest?.Message);
        }

        if (response.StatusCode == HttpStatusCode.Conflict)
        {
            var conflict = DeserializeOrDefault<ApiErrorResponse>(
                content);

            return CreateConflictResult(conflict?.Message);
        }

        return response.StatusCode switch
        {
            HttpStatusCode.Unauthorized =>
                new CreateUserApiResult
                {
                    IsUnauthorized = true,
                    ErrorMessage =
                        "Phiên đăng nhập đã hết hạn hoặc không hợp lệ."
                },
            HttpStatusCode.Forbidden =>
                new CreateUserApiResult
                {
                    IsForbidden = true,
                    ErrorMessage =
                        "Bạn không có quyền tạo tài khoản người dùng."
                },
            _ => new CreateUserApiResult
            {
                ErrorMessage =
                    "Không thể tạo tài khoản vào lúc này."
            }
        };
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

    public async Task<AdminUserApiResult> GetUserAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        const int pageSize = 100;
        var page = 1;

        while (true)
        {
            var result = await GetUsersAsync(
                new UserListApiQuery(
                    Search: null,
                    Role: null,
                    IsActive: null,
                    Page: page,
                    PageSize: pageSize),
                cancellationToken);

            if (!result.IsSuccess || result.Users is null)
            {
                return new AdminUserApiResult
                {
                    IsUnauthorized = result.IsUnauthorized,
                    IsForbidden = result.IsForbidden,
                    ErrorMessage = result.ErrorMessage ??
                        "Không thể tải thông tin tài khoản."
                };
            }

            var user = result.Users.Items.FirstOrDefault(
                item => item.Id == userId);

            if (user is not null)
            {
                return new AdminUserApiResult
                {
                    IsSuccess = true,
                    User = user
                };
            }

            if (!result.Users.HasNextPage)
            {
                return new AdminUserApiResult
                {
                    IsNotFound = true,
                    ErrorMessage =
                        "Tài khoản không còn tồn tại trong hệ thống."
                };
            }

            page++;
        }
    }

    public async Task<UpdateUserProfileApiResult> UpdateUserAsync(
        int userId,
        UpdateUserProfileApiRequest request,
        CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.PutAsJsonAsync(
            $"api/users/{userId.ToString(CultureInfo.InvariantCulture)}",
            request,
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.OK)
        {
            var user = await response.Content
                .ReadFromJsonAsync<UserListItemApiResponse>(
                    cancellationToken: cancellationToken);

            return new UpdateUserProfileApiResult
            {
                IsSuccess = user is not null,
                User = user,
                ErrorMessage = user is null
                    ? "API trả về tài khoản đã cập nhật không hợp lệ."
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
                return new UpdateUserProfileApiResult
                {
                    FieldErrors = validationProblem.Errors
                };
            }

            var badRequest = DeserializeOrDefault<ApiErrorResponse>(
                content);

            return UpdateUserBadRequestResult(
                badRequest?.Message);
        }

        if (response.StatusCode == HttpStatusCode.Conflict)
        {
            var conflict = DeserializeOrDefault<ApiErrorResponse>(
                content);

            return UpdateUserConflictResult(conflict?.Message);
        }

        return response.StatusCode switch
        {
            HttpStatusCode.Unauthorized =>
                new UpdateUserProfileApiResult
                {
                    IsUnauthorized = true,
                    ErrorMessage =
                        "Phiên đăng nhập đã hết hạn hoặc không hợp lệ."
                },
            HttpStatusCode.Forbidden =>
                new UpdateUserProfileApiResult
                {
                    IsForbidden = true,
                    ErrorMessage =
                        "Bạn không có quyền sửa tài khoản người dùng."
                },
            HttpStatusCode.NotFound =>
                new UpdateUserProfileApiResult
                {
                    IsNotFound = true,
                    ErrorMessage =
                        "Tài khoản không còn tồn tại trong hệ thống."
                },
            _ => new UpdateUserProfileApiResult
            {
                ErrorMessage =
                    "Không thể cập nhật tài khoản vào lúc này."
            }
        };
    }

    public async Task<UpdateUserActiveStatusApiResult>
        UpdateUserActiveStatusAsync(
            int userId,
            UpdateUserActiveStatusApiRequest request,
            CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.PutAsJsonAsync(
            $"api/users/{userId.ToString(CultureInfo.InvariantCulture)}" +
            "/active-status",
            request,
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.OK)
        {
            var user = await response.Content
                .ReadFromJsonAsync<UserActiveStatusApiResponse>(
                    cancellationToken: cancellationToken);

            return new UpdateUserActiveStatusApiResult
            {
                IsSuccess = user is not null,
                User = user,
                ErrorMessage = user is null
                    ? "API trả về trạng thái tài khoản không hợp lệ."
                    : null
            };
        }

        var content = await response.Content.ReadAsStringAsync(
            cancellationToken);
        var apiError = DeserializeOrDefault<ApiErrorResponse>(
            content);

        return response.StatusCode switch
        {
            HttpStatusCode.BadRequest
                when apiError?.Message ==
                    "Administrators cannot lock their own account." =>
                new UpdateUserActiveStatusApiResult
                {
                    IsSelfLock = true,
                    ErrorMessage =
                        "Bạn không thể khóa tài khoản Admin " +
                        "đang đăng nhập."
                },
            HttpStatusCode.BadRequest =>
                new UpdateUserActiveStatusApiResult
                {
                    ErrorMessage = apiError?.Message ??
                        "Trạng thái tài khoản không hợp lệ."
                },
            HttpStatusCode.Unauthorized =>
                new UpdateUserActiveStatusApiResult
                {
                    IsUnauthorized = true,
                    ErrorMessage =
                        "Phiên đăng nhập đã hết hạn hoặc không hợp lệ."
                },
            HttpStatusCode.Forbidden =>
                new UpdateUserActiveStatusApiResult
                {
                    IsForbidden = true,
                    ErrorMessage =
                        "Bạn không có quyền thay đổi trạng thái tài khoản."
                },
            HttpStatusCode.NotFound =>
                new UpdateUserActiveStatusApiResult
                {
                    IsNotFound = true,
                    ErrorMessage =
                        "Tài khoản không còn tồn tại trong hệ thống."
                },
            _ => new UpdateUserActiveStatusApiResult
            {
                ErrorMessage =
                    "Không thể thay đổi trạng thái tài khoản vào lúc này."
            }
        };
    }

    public async Task<UpdateUserRoleApiResult> UpdateUserRoleAsync(
        int userId,
        UpdateUserRoleApiRequest request,
        CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.PutAsJsonAsync(
            $"api/users/{userId.ToString(CultureInfo.InvariantCulture)}" +
            "/role",
            request,
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.OK)
        {
            var user = await response.Content
                .ReadFromJsonAsync<UserRoleApiResponse>(
                    cancellationToken: cancellationToken);

            return new UpdateUserRoleApiResult
            {
                IsSuccess = user is not null,
                User = user,
                ErrorMessage = user is null
                    ? "API trả về vai trò tài khoản không hợp lệ."
                    : null
            };
        }

        var content = await response.Content.ReadAsStringAsync(
            cancellationToken);
        var apiError = DeserializeOrDefault<ApiErrorResponse>(
            content);

        return response.StatusCode switch
        {
            HttpStatusCode.BadRequest
                when apiError?.Message ==
                    "The last active administrator cannot demote " +
                    "their own account." =>
                new UpdateUserRoleApiResult
                {
                    IsLastActiveAdmin = true,
                    ErrorMessage =
                        "Không thể hạ quyền Admin cuối cùng " +
                        "đang hoạt động."
                },
            HttpStatusCode.BadRequest =>
                new UpdateUserRoleApiResult
                {
                    ErrorMessage =
                        "Vai trò được chọn không hợp lệ."
                },
            HttpStatusCode.Unauthorized =>
                new UpdateUserRoleApiResult
                {
                    IsUnauthorized = true,
                    ErrorMessage =
                        "Phiên đăng nhập đã hết hạn hoặc không hợp lệ."
                },
            HttpStatusCode.Forbidden =>
                new UpdateUserRoleApiResult
                {
                    IsForbidden = true,
                    ErrorMessage =
                        "Bạn không có quyền thay đổi vai trò tài khoản."
                },
            HttpStatusCode.NotFound =>
                new UpdateUserRoleApiResult
                {
                    IsNotFound = true,
                    ErrorMessage =
                        "Tài khoản không còn tồn tại trong hệ thống."
                },
            _ => new UpdateUserRoleApiResult
            {
                ErrorMessage =
                    "Không thể thay đổi vai trò tài khoản vào lúc này."
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

    private static CreateUserApiResult CreateBadRequestResult(
        string? message)
    {
        if (message == "StudentCode is required for Student role.")
        {
            return CreateFieldErrorResult(
                "StudentCode",
                "Vui lòng nhập mã sinh viên cho tài khoản sinh viên.");
        }

        if (message == "Role is invalid.")
        {
            return CreateFieldErrorResult(
                "Role",
                "Vai trò không hợp lệ.");
        }

        if (message ==
            "FullName must contain at least 2 characters.")
        {
            return CreateFieldErrorResult(
                "FullName",
                "Họ và tên phải có ít nhất 2 ký tự.");
        }

        return new CreateUserApiResult
        {
            ErrorMessage = message ??
                "Thông tin tạo tài khoản không hợp lệ."
        };
    }

    private static CreateUserApiResult CreateConflictResult(
        string? message)
    {
        if (message == "Email is already registered.")
        {
            return CreateFieldErrorResult(
                "Email",
                "Email này đã được sử dụng.");
        }

        if (message == "StudentCode is already registered.")
        {
            return CreateFieldErrorResult(
                "StudentCode",
                "Mã sinh viên này đã được sử dụng.");
        }

        return new CreateUserApiResult
        {
            ErrorMessage = message ??
                "Email hoặc mã sinh viên đã tồn tại."
        };
    }

    private static CreateUserApiResult CreateFieldErrorResult(
        string field,
        string message)
    {
        return new CreateUserApiResult
        {
            FieldErrors = new Dictionary<string, string[]>
            {
                [field] = [message]
            }
        };
    }

    private static UpdateUserProfileApiResult
        UpdateUserBadRequestResult(string? message)
    {
        if (message == "StudentCode is required for Student role.")
        {
            return UpdateUserFieldErrorResult(
                "StudentCode",
                "Vui lòng nhập mã sinh viên cho tài khoản sinh viên.");
        }

        if (message ==
            "FullName must contain at least 2 characters.")
        {
            return UpdateUserFieldErrorResult(
                "FullName",
                "Họ và tên phải có ít nhất 2 ký tự.");
        }

        return new UpdateUserProfileApiResult
        {
            ErrorMessage = message ??
                "Thông tin cập nhật tài khoản không hợp lệ."
        };
    }

    private static UpdateUserProfileApiResult
        UpdateUserConflictResult(string? message)
    {
        if (message == "Email is already registered.")
        {
            return UpdateUserFieldErrorResult(
                "Email",
                "Email này đã được sử dụng.");
        }

        if (message == "StudentCode is already registered.")
        {
            return UpdateUserFieldErrorResult(
                "StudentCode",
                "Mã sinh viên này đã được sử dụng.");
        }

        return new UpdateUserProfileApiResult
        {
            ErrorMessage = message ??
                "Email hoặc mã sinh viên đã tồn tại."
        };
    }

    private static UpdateUserProfileApiResult
        UpdateUserFieldErrorResult(
            string field,
            string message)
    {
        return new UpdateUserProfileApiResult
        {
            FieldErrors = new Dictionary<string, string[]>
            {
                [field] = [message]
            }
        };
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
