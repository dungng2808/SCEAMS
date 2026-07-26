using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using SCEAMS.MVC.Models.Api;

namespace SCEAMS.MVC.Services.ApiClients;

public sealed class ClubMembershipApiClient : IClubMembershipApiClient
{
    private readonly HttpClient _httpClient;

    public ClubMembershipApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<PendingMembershipsApiResult> GetPendingMembershipsAsync(
        int clubId,
        string? search,
        int page = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var normalizedPage = Math.Max(page, 1);
        var normalizedPageSize = Math.Clamp(pageSize, 1, 50);

        var queryString = $"page={normalizedPage}&pageSize={normalizedPageSize}";
        if (!string.IsNullOrWhiteSpace(search))
        {
            queryString += $"&search={Uri.EscapeDataString(search.Trim())}";
        }

        var requestUri = $"api/clubs/{clubId}/members/pending?{queryString}";

        using var response = await _httpClient.GetAsync(requestUri, cancellationToken);

        if (response.StatusCode == HttpStatusCode.OK)
        {
            var pagedResponse = await response.Content
                .ReadFromJsonAsync<PagedApiResponse<ClubMembershipApiResponse>>(
                    cancellationToken: cancellationToken);

            var items = pagedResponse?.Items ?? [];
            return new PendingMembershipsApiResult
            {
                IsSuccess = true,
                Items = items,
                TotalItems = pagedResponse?.TotalItems ?? 0,
                Page = normalizedPage,
                PageSize = normalizedPageSize
            };
        }

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return new PendingMembershipsApiResult
            {
                IsNotFound = true,
                ErrorMessage = $"Không tìm thấy câu lạc bộ #{clubId}."
            };
        }

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            return new PendingMembershipsApiResult
            {
                IsUnauthorized = true,
                ErrorMessage = "Phiên đăng nhập đã hết hạn hoặc không hợp lệ."
            };
        }

        if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            return new PendingMembershipsApiResult
            {
                IsForbidden = true,
                ErrorMessage = "Bạn không có quyền xem danh sách đơn xin gia nhập câu lạc bộ này."
            };
        }

        return new PendingMembershipsApiResult
        {
            ErrorMessage = "Không thể tải danh sách đơn xin gia nhập vào lúc này."
        };
    }

    private sealed record PagedApiResponse<T>(List<T>? Items, int TotalItems);
}
