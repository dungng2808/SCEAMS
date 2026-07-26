using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using SCEAMS.MVC.Models.Api;

namespace SCEAMS.MVC.Services.ApiClients;

public sealed class ClubApiClient : IClubApiClient
{
    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;

    public ClubApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<ClubListApiResult> GetClubsAsync(
        ClubListApiQuery query,
        CancellationToken cancellationToken = default)
    {
        var filterConditions = new List<string>();

        if (query.CategoryId.HasValue && query.CategoryId.Value > 0)
        {
            filterConditions.Add($"CategoryId eq {query.CategoryId.Value}");
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var sanitizedSearch = query.Search.Trim().Replace("'", "''");
            filterConditions.Add(
                $"contains(tolower(Name), '{sanitizedSearch.ToLowerInvariant()}')");
        }

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            var allowedStatuses = new[] { "PendingApproval", "Approved", "Rejected", "Dissolved" };
            if (allowedStatuses.Contains(query.Status, StringComparer.OrdinalIgnoreCase))
            {
                filterConditions.Add($"Status eq '{query.Status}'");
            }
        }

        var queryStringParams = new List<string>();

        if (filterConditions.Count > 0)
        {
            var combinedFilter = string.Join(" and ", filterConditions);
            queryStringParams.Add($"$filter={Uri.EscapeDataString(combinedFilter)}");
        }

        var orderBy = NormalizeOrderBy(query.OrderBy);
        if (!string.IsNullOrWhiteSpace(orderBy))
        {
            queryStringParams.Add($"$orderby={Uri.EscapeDataString(orderBy)}");
        }

        var page = Math.Max(query.Page, 1);
        var pageSize = Math.Clamp(query.PageSize, 1, 50);
        var skip = (page - 1) * pageSize;

        queryStringParams.Add($"$top={pageSize}");
        queryStringParams.Add($"$skip={skip}");

        var requestUri = "api/clubs";
        if (queryStringParams.Count > 0)
        {
            requestUri += "?" + string.Join("&", queryStringParams);
        }

        using var response = await _httpClient.GetAsync(requestUri, cancellationToken);

        if (response.StatusCode == HttpStatusCode.OK)
        {
            var clubs = await response.Content
                .ReadFromJsonAsync<List<ClubApiResponse>>(
                    cancellationToken: cancellationToken);

            var items = clubs ?? [];
            return new ClubListApiResult
            {
                IsSuccess = true,
                Clubs = items,
                TotalItems = items.Count,
                Page = page,
                PageSize = pageSize
            };
        }

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            return new ClubListApiResult
            {
                IsUnauthorized = true,
                ErrorMessage = "Phiên đăng nhập đã hết hạn hoặc không hợp lệ."
            };
        }

        if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            return new ClubListApiResult
            {
                IsForbidden = true,
                ErrorMessage = "Bạn không có quyền truy cập danh sách câu lạc bộ."
            };
        }

        return new ClubListApiResult
        {
            ErrorMessage = "Không thể tải danh sách câu lạc bộ vào lúc này."
        };
    }

    private static string NormalizeOrderBy(string? orderBy)
    {
        return orderBy?.ToLowerInvariant() switch
        {
            "name_desc" => "Name desc",
            "created_desc" => "CreatedAt desc",
            "created_asc" => "CreatedAt asc",
            "members_desc" => "ActiveMemberCount desc",
            "name_asc" or _ => "Name asc"
        };
    }
}
