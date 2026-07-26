using System.Net;
using System.Net.Http.Json;
using SCEAMS.MVC.Models.Api;

namespace SCEAMS.MVC.Services.ApiClients;

public sealed class VenueApiClient : IVenueApiClient
{
    private readonly HttpClient _httpClient;

    public VenueApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<VenueListApiResult> GetVenuesAsync(
        string? search,
        bool? maintenance,
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

        if (!string.IsNullOrWhiteSpace(search))
        {
            query.Insert(0, $"search={Uri.EscapeDataString(search.Trim())}");
        }

        if (maintenance.HasValue)
        {
            query.Add($"maintenance={maintenance.Value.ToString().ToLowerInvariant()}");
        }

        using var response = await _httpClient.GetAsync(
            $"api/venues?{string.Join("&", query)}",
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.OK)
        {
            var pagedResponse = await response.Content
                .ReadFromJsonAsync<PagedApiResponse<VenueApiResponse>>(
                    cancellationToken: cancellationToken);

            return new VenueListApiResult
            {
                IsSuccess = true,
                Venues = pagedResponse?.Items ?? [],
                TotalItems = pagedResponse?.TotalItems ?? 0,
                Page = normalizedPage,
                PageSize = normalizedPageSize
            };
        }

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            return new VenueListApiResult
            {
                IsUnauthorized = true,
                ErrorMessage = "Phiên đăng nhập đã hết hạn hoặc không hợp lệ."
            };
        }

        if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            return new VenueListApiResult
            {
                IsForbidden = true,
                ErrorMessage = "Bạn không có quyền xem danh sách địa điểm."
            };
        }

        return new VenueListApiResult
        {
            ErrorMessage = "Không thể tải danh sách địa điểm vào lúc này."
        };
    }

    private sealed record PagedApiResponse<T>(List<T>? Items, int TotalItems);
}
