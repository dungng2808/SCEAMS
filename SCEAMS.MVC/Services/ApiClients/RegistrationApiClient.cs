using System.Net;
using System.Net.Http.Json;
using SCEAMS.MVC.Models.Api;

namespace SCEAMS.MVC.Services.ApiClients;

public sealed class RegistrationApiClient : IRegistrationApiClient
{
    private readonly HttpClient _httpClient;

    public RegistrationApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<RegistrationHistoryApiResult> GetMyRegistrationsAsync(
        string? status,
        int page = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var normalizedPage = Math.Max(page, 1);
        var normalizedPageSize = Math.Clamp(pageSize, 1, 50);
        var query = new List<string>
        {
            $"page={normalizedPage}",
            $"pageSize={normalizedPageSize}"
        };
        if (!string.IsNullOrWhiteSpace(status))
        {
            query.Insert(0, $"status={Uri.EscapeDataString(status.Trim())}");
        }

        using var response = await _httpClient.GetAsync(
            $"api/registrations/me?{string.Join("&", query)}",
            cancellationToken);
        if (response.StatusCode == HttpStatusCode.OK)
        {
            var payload = await response.Content
                .ReadFromJsonAsync<PagedRegistrationApiResponse>(
                    cancellationToken: cancellationToken);
            return new RegistrationHistoryApiResult
            {
                IsSuccess = true,
                Items = payload?.Items ?? [],
                TotalItems = payload?.TotalItems ?? 0,
                Page = normalizedPage,
                PageSize = normalizedPageSize
            };
        }

        var errorMessage = await TryReadErrorMessageAsync(response, cancellationToken);
        return response.StatusCode switch
        {
            HttpStatusCode.Unauthorized => new RegistrationHistoryApiResult
            {
                IsUnauthorized = true,
                ErrorMessage = "Phiên đăng nhập đã hết hạn hoặc không hợp lệ."
            },
            HttpStatusCode.Forbidden => new RegistrationHistoryApiResult
            {
                IsForbidden = true,
                ErrorMessage = errorMessage ?? "Bạn không có quyền xem lịch sử đăng ký."
            },
            HttpStatusCode.BadRequest => new RegistrationHistoryApiResult
            {
                IsBadRequest = true,
                ErrorMessage = errorMessage ?? "Bộ lọc registration không hợp lệ."
            },
            _ => new RegistrationHistoryApiResult
            {
                ErrorMessage = errorMessage ?? "Không thể tải lịch sử đăng ký."
            }
        };
    }

    private static async Task<string?> TryReadErrorMessageAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        try
        {
            var payload = await response.Content
                .ReadFromJsonAsync<ErrorResponse>(cancellationToken: cancellationToken);
            return payload?.Message;
        }
        catch (System.Text.Json.JsonException)
        {
            return null;
        }
    }

    private sealed record PagedRegistrationApiResponse(
        List<RegistrationHistoryApiResponse>? Items,
        int TotalItems);

    private sealed record ErrorResponse(string? Message);
}
