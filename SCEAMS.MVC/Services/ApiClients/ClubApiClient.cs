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

    public async Task<ClubDetailApiResult> GetClubByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.GetAsync($"api/clubs/{id}", cancellationToken);

        if (response.StatusCode == HttpStatusCode.OK)
        {
            var club = await response.Content
                .ReadFromJsonAsync<ClubDetailApiResponse>(
                    cancellationToken: cancellationToken);

            return new ClubDetailApiResult
            {
                IsSuccess = true,
                Club = club
            };
        }

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return new ClubDetailApiResult
            {
                IsNotFound = true,
                ErrorMessage = $"Không tìm thấy câu lạc bộ với mã #{id} hoặc bạn không có quyền xem."
            };
        }

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            return new ClubDetailApiResult
            {
                IsUnauthorized = true,
                ErrorMessage = "Phiên đăng nhập đã hết hạn hoặc không hợp lệ."
            };
        }

        if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            return new ClubDetailApiResult
            {
                IsForbidden = true,
                ErrorMessage = "Bạn không có quyền xem thông tin câu lạc bộ này."
            };
        }

        return new ClubDetailApiResult
        {
            ErrorMessage = "Không thể tải chi tiết câu lạc bộ vào lúc này."
        };
    }

    public async Task<CreateClubApiResult> CreateClubAsync(
        CreateClubApiRequest request,
        CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.PostAsJsonAsync("api/clubs", request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.Created)
        {
            var createdClub = await response.Content
                .ReadFromJsonAsync<ClubDetailApiResponse>(
                    cancellationToken: cancellationToken);

            return new CreateClubApiResult
            {
                IsSuccess = true,
                Club = createdClub
            };
        }

        if (response.StatusCode == HttpStatusCode.Conflict)
        {
            var message = await TryReadErrorMessageAsync(response, cancellationToken);
            return new CreateClubApiResult
            {
                IsConflict = true,
                ErrorMessage = message ?? "Tên câu lạc bộ đã tồn tại."
            };
        }

        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            var message = await TryReadErrorMessageAsync(response, cancellationToken);
            return new CreateClubApiResult
            {
                ErrorMessage = message ?? "Dữ liệu không hợp lệ. Vui lòng kiểm tra lại."
            };
        }

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            return new CreateClubApiResult
            {
                IsUnauthorized = true,
                ErrorMessage = "Phiên đăng nhập đã hết hạn hoặc không hợp lệ."
            };
        }

        if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            return new CreateClubApiResult
            {
                IsForbidden = true,
                ErrorMessage = "Bạn không có quyền thực hiện đề xuất câu lạc bộ."
            };
        }

        return new CreateClubApiResult
        {
            ErrorMessage = "Không thể gửi đề xuất câu lạc bộ vào lúc này."
        };
    }

    public async Task<ApproveClubApiResult> ApproveClubAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.PutAsync($"api/clubs/{id}/approve", null, cancellationToken);

        if (response.StatusCode == HttpStatusCode.OK)
        {
            var approvedClub = await response.Content
                .ReadFromJsonAsync<ClubDetailApiResponse>(
                    cancellationToken: cancellationToken);

            return new ApproveClubApiResult
            {
                IsSuccess = true,
                Club = approvedClub
            };
        }

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return new ApproveClubApiResult
            {
                IsNotFound = true,
                ErrorMessage = $"Không tìm thấy câu lạc bộ #{id}."
            };
        }

        if (response.StatusCode == HttpStatusCode.Conflict)
        {
            var message = await TryReadErrorMessageAsync(response, cancellationToken);
            return new ApproveClubApiResult
            {
                IsConflict = true,
                ErrorMessage = message ?? "Chỉ câu lạc bộ ở trạng thái Chờ duyệt mới có thể phê duyệt."
            };
        }

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            return new ApproveClubApiResult
            {
                IsUnauthorized = true,
                ErrorMessage = "Phiên đăng nhập đã hết hạn hoặc không hợp lệ."
            };
        }

        if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            return new ApproveClubApiResult
            {
                IsForbidden = true,
                ErrorMessage = "Bạn không có quyền thực hiện duyệt câu lạc bộ."
            };
        }

        return new ApproveClubApiResult
        {
            ErrorMessage = "Không thể duyệt câu lạc bộ vào lúc này."
        };
    }

    private static async Task<string?> TryReadErrorMessageAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        try
        {
            var body = await response.Content
                .ReadFromJsonAsync<JsonElement>(
                    cancellationToken: cancellationToken);

            if (body.ValueKind == JsonValueKind.Object)
            {
                if (body.TryGetProperty("message", out var msgProp) && msgProp.ValueKind == JsonValueKind.String)
                {
                    return msgProp.GetString();
                }
                if (body.TryGetProperty("detail", out var detailProp) && detailProp.ValueKind == JsonValueKind.String)
                {
                    return detailProp.GetString();
                }
            }
        }
        catch
        {
            // Fallback
        }

        return null;
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
