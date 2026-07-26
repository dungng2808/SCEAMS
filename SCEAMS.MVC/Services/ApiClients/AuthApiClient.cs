using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using SCEAMS.MVC.Models.Api;

namespace SCEAMS.MVC.Services.ApiClients;

public sealed class AuthApiClient : IAuthApiClient
{
    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;

    public AuthApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<LoginApiResult> LoginAsync(
        LoginApiRequest request,
        CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.PostAsJsonAsync(
            "api/auth/login",
            request,
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.OK)
        {
            var loginResponse = await response.Content
                .ReadFromJsonAsync<LoginApiResponse>(
                    cancellationToken: cancellationToken);

            return new LoginApiResult
            {
                IsSuccess = loginResponse is not null,
                Response = loginResponse,
                ErrorMessage = loginResponse is null
                    ? "API trả về dữ liệu đăng nhập không hợp lệ."
                    : null
            };
        }

        var content = await response.Content.ReadAsStringAsync(
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            var validationProblem =
                JsonSerializer.Deserialize<ValidationProblemApiResponse>(
                    content,
                    JsonOptions);

            if (validationProblem?.Errors is { Count: > 0 })
            {
                return new LoginApiResult
                {
                    FieldErrors = validationProblem.Errors
                };
            }
        }

        return new LoginApiResult
        {
            ErrorMessage = response.StatusCode switch
            {
                HttpStatusCode.Unauthorized =>
                    "Email hoặc mật khẩu không chính xác.",
                HttpStatusCode.Forbidden =>
                    "Tài khoản đã bị khóa. Vui lòng liên hệ quản trị viên.",
                _ => "Không thể đăng nhập vào lúc này."
            }
        };
    }

    public async Task<RegisterStudentApiResult> RegisterStudentAsync(
        RegisterStudentApiRequest request,
        CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.PostAsJsonAsync(
            "api/auth/register",
            request,
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.Created)
        {
            var student = await response.Content
                .ReadFromJsonAsync<RegisteredStudentApiResponse>(
                    cancellationToken: cancellationToken);

            return new RegisterStudentApiResult
            {
                IsSuccess = student is not null,
                Student = student,
                ErrorMessage = student is null
                    ? "API trả về dữ liệu đăng ký không hợp lệ."
                    : null
            };
        }

        var content = await response.Content.ReadAsStringAsync(
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            var validationProblem =
                JsonSerializer.Deserialize<ValidationProblemApiResponse>(
                    content,
                    JsonOptions);

            if (validationProblem?.Errors is { Count: > 0 })
            {
                return new RegisterStudentApiResult
                {
                    FieldErrors = validationProblem.Errors
                };
            }
        }

        var apiError = JsonSerializer.Deserialize<ApiErrorResponse>(
            content,
            JsonOptions);

        if (response.StatusCode == HttpStatusCode.Conflict &&
            apiError?.Message is not null)
        {
            var fieldName = apiError.Message.StartsWith(
                "Email",
                StringComparison.OrdinalIgnoreCase)
                ? "Email"
                : apiError.Message.StartsWith(
                    "StudentCode",
                    StringComparison.OrdinalIgnoreCase)
                    ? "StudentCode"
                    : string.Empty;

            if (fieldName.Length > 0)
            {
                return new RegisterStudentApiResult
                {
                    FieldErrors = new Dictionary<string, string[]>
                    {
                        [fieldName] = [TranslateConflict(apiError.Message)]
                    }
                };
            }
        }

        return new RegisterStudentApiResult
        {
            ErrorMessage = apiError?.Message ??
                "Không thể đăng ký tài khoản vào lúc này."
        };
    }

    public async Task<RefreshTokenApiResult> RefreshTokenAsync(
        RefreshTokenApiRequest request,
        CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.PostAsJsonAsync(
            "api/auth/refresh",
            request,
            cancellationToken);

        if (response.StatusCode != HttpStatusCode.OK)
        {
            return new RefreshTokenApiResult();
        }

        var tokenResponse = await response.Content
            .ReadFromJsonAsync<RefreshTokenApiResponse>(
                cancellationToken: cancellationToken);

        return new RefreshTokenApiResult
        {
            IsSuccess = tokenResponse is not null,
            Response = tokenResponse
        };
    }

    public async Task<bool> RevokeTokenAsync(
        RefreshTokenApiRequest request,
        CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.PostAsJsonAsync(
            "api/auth/revoke",
            request,
            cancellationToken);

        return response.StatusCode ==
            HttpStatusCode.NoContent;
    }

    private static string TranslateConflict(string message)
    {
        return message.StartsWith(
            "Email",
            StringComparison.OrdinalIgnoreCase)
            ? "Email này đã được đăng ký."
            : "Mã sinh viên này đã được đăng ký.";
    }

    private sealed record ApiErrorResponse(string? Message);

    private sealed record ValidationProblemApiResponse(
        Dictionary<string, string[]> Errors);
}
