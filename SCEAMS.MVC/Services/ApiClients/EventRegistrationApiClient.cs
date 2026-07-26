using System.Net;
using System.Net.Http.Json;
using SCEAMS.MVC.Models.Api;

namespace SCEAMS.MVC.Services.ApiClients;

public sealed class EventRegistrationApiClient : IEventRegistrationApiClient
{
    private readonly HttpClient _httpClient;

    public EventRegistrationApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<EventRegistrationListApiResult> GetForEventAsync(
        int eventId,
        string? status,
        string? search,
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

        if (!string.IsNullOrWhiteSpace(search))
        {
            query.Insert(0, $"search={Uri.EscapeDataString(search.Trim())}");
        }

        using var response = await _httpClient.GetAsync(
            $"api/events/{eventId}/registrations?{string.Join("&", query)}",
            cancellationToken);
        if (response.StatusCode == HttpStatusCode.OK)
        {
            var payload = await response.Content
                .ReadFromJsonAsync<PagedEventRegistrationApiResponse>(
                    cancellationToken: cancellationToken);
            return new EventRegistrationListApiResult
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
            HttpStatusCode.Unauthorized => new EventRegistrationListApiResult
            {
                IsUnauthorized = true,
                ErrorMessage = "Phiên đăng nhập đã hết hạn hoặc không hợp lệ."
            },
            HttpStatusCode.Forbidden => new EventRegistrationListApiResult
            {
                IsForbidden = true,
                ErrorMessage = errorMessage ?? "Bạn không có quyền xem registration của Event này."
            },
            HttpStatusCode.NotFound => new EventRegistrationListApiResult
            {
                IsNotFound = true,
                ErrorMessage = errorMessage ?? "Event không tồn tại."
            },
            HttpStatusCode.BadRequest => new EventRegistrationListApiResult
            {
                IsBadRequest = true,
                ErrorMessage = errorMessage ?? "Bộ lọc registration không hợp lệ."
            },
            _ => new EventRegistrationListApiResult
            {
                ErrorMessage = errorMessage ?? "Không thể tải danh sách registration."
            }
        };
    }

    public async Task<CheckInApiResult> CheckInAsync(
        int registrationId,
        CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.PutAsync(
            $"api/registrations/{registrationId}/check-in",
            content: null,
            cancellationToken);
        if (response.StatusCode == HttpStatusCode.OK)
        {
            var payload = await response.Content
                .ReadFromJsonAsync<CheckInApiResponse>(
                    cancellationToken: cancellationToken);
            return new CheckInApiResult
            {
                IsSuccess = payload is not null,
                RegistrationId = payload?.RegistrationId ?? registrationId,
                CheckInTime = payload?.CheckInTime ?? DateTime.UtcNow,
                CheckedInByUserId = payload?.CheckedInByUserId ?? 0,
                ErrorMessage = payload is null ? "API trả về kết quả điểm danh không hợp lệ." : null
            };
        }

        var errorMessage = await TryReadErrorMessageAsync(response, cancellationToken);
        return response.StatusCode switch
        {
            HttpStatusCode.Unauthorized => new CheckInApiResult
            {
                IsUnauthorized = true,
                ErrorMessage = "Phiên đăng nhập đã hết hạn hoặc không hợp lệ."
            },
            HttpStatusCode.Forbidden => new CheckInApiResult
            {
                IsForbidden = true,
                ErrorMessage = errorMessage ?? "Bạn không có quyền điểm danh."
            },
            HttpStatusCode.NotFound => new CheckInApiResult
            {
                IsNotFound = true,
                ErrorMessage = errorMessage ?? "Registration không tồn tại."
            },
            HttpStatusCode.Conflict => new CheckInApiResult
            {
                IsConflict = true,
                ErrorMessage = errorMessage ?? "Registration không thể điểm danh ở thời điểm hiện tại."
            },
            _ => new CheckInApiResult
            {
                ErrorMessage = errorMessage ?? "Không thể điểm danh registration."
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

    private sealed record PagedEventRegistrationApiResponse(
        List<EventRegistrationApiResponse>? Items,
        int TotalItems);

    private sealed record ErrorResponse(string? Message);

    private sealed record CheckInApiResponse(
        int RegistrationId,
        int EventId,
        string Status,
        DateTime CheckInTime,
        int CheckedInByUserId);
}
