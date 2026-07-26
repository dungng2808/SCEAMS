using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using SCEAMS.MVC.Models.Api;

namespace SCEAMS.MVC.Services.ApiClients;

public sealed class EventApiClient : IEventApiClient
{
    private readonly HttpClient _httpClient;

    public EventApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<EventDetailApiResult> GetEventByIdAsync(
        int eventId,
        CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.GetAsync(
            $"api/events/{eventId}",
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.OK)
        {
            var eventItem = await response.Content
                .ReadFromJsonAsync<EventDetailApiResponse>(
                    cancellationToken: cancellationToken);
            return new EventDetailApiResult
            {
                IsSuccess = eventItem is not null,
                Event = eventItem,
                ErrorMessage = eventItem is null
                    ? "API trả về chi tiết Event không hợp lệ."
                    : null
            };
        }

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return new EventDetailApiResult
            {
                IsNotFound = true,
                ErrorMessage = "Event không tồn tại hoặc bạn không có quyền xem."
            };
        }

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            return new EventDetailApiResult
            {
                IsUnauthorized = true,
                ErrorMessage = "Phiên đăng nhập đã hết hạn hoặc không hợp lệ."
            };
        }

        if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            return new EventDetailApiResult
            {
                IsForbidden = true,
                ErrorMessage = "Bạn không có quyền xem Event này."
            };
        }

        return new EventDetailApiResult
        {
            ErrorMessage = "Không thể tải chi tiết Event vào lúc này."
        };
    }

    public async Task<EventListApiResult> GetEventsAsync(
        string? search,
        int? clubId,
        DateTime? from,
        DateTime? to,
        string? status,
        bool? hasSlots,
        int page = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var normalizedPage = Math.Max(page, 1);
        var normalizedPageSize = Math.Clamp(pageSize, 1, 50);
        var filters = new List<string>();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var escapedSearch = search.Trim()
                .Replace("'", "''")
                .ToLowerInvariant();
            filters.Add(
                $"(contains(tolower(Title), '{escapedSearch}') or contains(tolower(ClubName), '{escapedSearch}') or contains(tolower(VenueName), '{escapedSearch}'))");
        }

        if (clubId is > 0)
        {
            filters.Add($"ClubId eq {clubId.Value}");
        }

        if (from.HasValue)
        {
            filters.Add($"StartTime ge {FormatDate(from.Value)}");
        }

        if (to.HasValue)
        {
            filters.Add($"StartTime lt {FormatDate(to.Value.Date.AddDays(1))}");
        }

        var allowedStatuses = new HashSet<string>(
            ["Draft", "PendingApproval", "Approved", "Ongoing", "Completed", "Cancelled", "Rejected"],
            StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrWhiteSpace(status) && allowedStatuses.Contains(status))
        {
            filters.Add($"Status eq '{status}'");
        }

        if (hasSlots == true)
        {
            filters.Add("SlotsRemaining gt 0");
        }
        else if (hasSlots == false)
        {
            filters.Add("SlotsRemaining eq 0");
        }

        var query = new List<string>();
        if (filters.Count > 0)
        {
            query.Add($"$filter={Uri.EscapeDataString(string.Join(" and ", filters))}");
        }

        query.Add($"$orderby={Uri.EscapeDataString("StartTime asc")}");
        query.Add($"$skip={(normalizedPage - 1) * normalizedPageSize}");
        query.Add($"$top={normalizedPageSize}");
        query.Add("$count=true");

        using var response = await _httpClient.GetAsync(
            $"api/events?{string.Join("&", query)}",
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.OK)
        {
            var payload = await response.Content
                .ReadFromJsonAsync<EventODataListApiResponse>(
                    cancellationToken: cancellationToken);
            var events = payload?.Value ?? [];
            return new EventListApiResult
            {
                IsSuccess = true,
                Events = events,
                TotalItems = payload?.Count ?? events.Count,
                Page = normalizedPage,
                PageSize = normalizedPageSize
            };
        }

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            return new EventListApiResult
            {
                IsUnauthorized = true,
                ErrorMessage = "Phiên đăng nhập đã hết hạn hoặc không hợp lệ."
            };
        }

        if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            return new EventListApiResult
            {
                IsForbidden = true,
                ErrorMessage = "Bạn không có quyền xem danh sách sự kiện."
            };
        }

        return new EventListApiResult
        {
            ErrorMessage = "Không thể tải danh sách sự kiện vào lúc này."
        };
    }

    private static string FormatDate(DateTime dateTime)
    {
        return dateTime.ToUniversalTime()
            .ToString("O", CultureInfo.InvariantCulture);
    }
}
