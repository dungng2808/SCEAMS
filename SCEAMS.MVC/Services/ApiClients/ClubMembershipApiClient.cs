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

    public async Task<PendingMembershipsApiResult> GetActiveMembershipsAsync(
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

        using var response = await _httpClient.GetAsync(
            $"api/clubs/{clubId}/members/active?{queryString}",
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.OK)
        {
            var pagedResponse = await response.Content
                .ReadFromJsonAsync<PagedApiResponse<ClubMembershipApiResponse>>(
                    cancellationToken: cancellationToken);

            return new PendingMembershipsApiResult
            {
                IsSuccess = true,
                Items = pagedResponse?.Items ?? [],
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
                ErrorMessage = "Bạn không có quyền xem danh sách thành viên của câu lạc bộ này."
            };
        }

        return new PendingMembershipsApiResult
        {
            ErrorMessage = "Không thể tải danh sách thành viên vào lúc này."
        };
    }

    public async Task<DecideClubMembershipApiResult> DecideMembershipAsync(
        int clubId,
        int userId,
        DecideClubMembershipApiRequest request,
        CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.PutAsJsonAsync(
            $"api/clubs/{clubId}/members/{userId}/decision",
            request,
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.OK)
        {
            var membership = await response.Content
                .ReadFromJsonAsync<ClubMembershipApiResponse>(
                    cancellationToken: cancellationToken);

            return new DecideClubMembershipApiResult
            {
                IsSuccess = true,
                Membership = membership
            };
        }

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return new DecideClubMembershipApiResult
            {
                IsNotFound = true,
                ErrorMessage = "Không tìm thấy câu lạc bộ hoặc đơn gia nhập cần xử lý."
            };
        }

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            return new DecideClubMembershipApiResult
            {
                IsUnauthorized = true,
                ErrorMessage = "Phiên đăng nhập đã hết hạn hoặc không hợp lệ."
            };
        }

        if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            return new DecideClubMembershipApiResult
            {
                IsForbidden = true,
                ErrorMessage = "Bạn không có quyền xử lý đơn gia nhập của câu lạc bộ này."
            };
        }

        if (response.StatusCode == HttpStatusCode.Conflict)
        {
            return new DecideClubMembershipApiResult
            {
                IsConflict = true,
                ErrorMessage = "Đơn gia nhập này đã được xử lý bởi người khác hoặc không còn ở trạng thái chờ duyệt."
            };
        }

        return new DecideClubMembershipApiResult
        {
            ErrorMessage = "Không thể xử lý đơn gia nhập vào lúc này."
        };
    }

    public async Task<RemoveClubMembershipApiResult> RemoveMembershipAsync(
        int clubId,
        int userId,
        RemoveClubMembershipApiRequest request,
        CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.PutAsJsonAsync(
            $"api/clubs/{clubId}/members/{userId}/remove",
            request,
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.OK)
        {
            var membership = await response.Content
                .ReadFromJsonAsync<ClubMembershipApiResponse>(
                    cancellationToken: cancellationToken);

            return new RemoveClubMembershipApiResult
            {
                IsSuccess = true,
                Membership = membership
            };
        }

        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            return new RemoveClubMembershipApiResult
            {
                IsValidationError = true,
                ErrorMessage = "Lý do loại thành viên không hợp lệ."
            };
        }

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return new RemoveClubMembershipApiResult
            {
                IsNotFound = true,
                ErrorMessage = "Không tìm thấy câu lạc bộ hoặc thành viên cần loại."
            };
        }

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            return new RemoveClubMembershipApiResult
            {
                IsUnauthorized = true,
                ErrorMessage = "Phiên đăng nhập đã hết hạn hoặc không hợp lệ."
            };
        }

        if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            return new RemoveClubMembershipApiResult
            {
                IsForbidden = true,
                ErrorMessage = "Bạn không có quyền loại thành viên khỏi câu lạc bộ này."
            };
        }

        if (response.StatusCode == HttpStatusCode.Conflict)
        {
            return new RemoveClubMembershipApiResult
            {
                IsConflict = true,
                ErrorMessage = "Thành viên này không còn ở trạng thái Active hoặc đã được xử lý trước đó."
            };
        }

        return new RemoveClubMembershipApiResult
        {
            ErrorMessage = "Không thể loại thành viên vào lúc này."
        };
    }

    private sealed record PagedApiResponse<T>(List<T>? Items, int TotalItems);
}
