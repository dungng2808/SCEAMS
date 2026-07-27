using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using SCEAMS.MVC.Models.Api;

namespace SCEAMS.MVC.Services.ApiClients;

public sealed class ReportApiClient : IReportApiClient
{
    private readonly HttpClient _httpClient;

    public ReportApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<EventSummaryReportApiResult> GetEventSummaryAsync(
        DateTime? from,
        DateTime? to,
        CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.GetAsync(
            BuildQuery("api/reports/event-summary", from, to),
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.OK)
        {
            var report = await response.Content
                .ReadFromJsonAsync<EventSummaryReportApiResponse>(
                    cancellationToken: cancellationToken);
            return new EventSummaryReportApiResult
            {
                IsSuccess = report is not null,
                Report = report,
                ErrorMessage = report is null
                    ? "API trả về báo cáo Event không hợp lệ."
                    : null
            };
        }

        var message = await ExtractMessageAsync(response, cancellationToken);
        return response.StatusCode switch
        {
            HttpStatusCode.Unauthorized => new EventSummaryReportApiResult
            {
                IsUnauthorized = true,
                ErrorMessage = "Phiên đăng nhập đã hết hạn hoặc không hợp lệ."
            },
            HttpStatusCode.Forbidden => new EventSummaryReportApiResult
            {
                IsForbidden = true,
                ErrorMessage = message ?? "Bạn không có quyền xem báo cáo Event."
            },
            HttpStatusCode.BadRequest => new EventSummaryReportApiResult
            {
                IsBadRequest = true,
                ErrorMessage = message ?? "Khoảng thời gian báo cáo không hợp lệ."
            },
            _ => new EventSummaryReportApiResult
            {
                ErrorMessage = message ?? "Không thể tải báo cáo Event."
            }
        };
    }

    public async Task<ClubActivityReportApiResult> GetClubActivityAsync(
        DateTime? from,
        DateTime? to,
        CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.GetAsync(
            BuildQuery("api/reports/club-activity", from, to),
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.OK)
        {
            var report = await response.Content
                .ReadFromJsonAsync<ClubActivityReportApiResponse>(
                    cancellationToken: cancellationToken);
            return new ClubActivityReportApiResult
            {
                IsSuccess = report is not null,
                Report = report,
                ErrorMessage = report is null
                    ? "API trả về báo cáo hoạt động CLB không hợp lệ."
                    : null
            };
        }

        var message = await ExtractMessageAsync(response, cancellationToken);
        return response.StatusCode switch
        {
            HttpStatusCode.Unauthorized => new ClubActivityReportApiResult
            {
                IsUnauthorized = true,
                ErrorMessage = "Phiên đăng nhập đã hết hạn hoặc không hợp lệ."
            },
            HttpStatusCode.Forbidden => new ClubActivityReportApiResult
            {
                IsForbidden = true,
                ErrorMessage = message ?? "Bạn không có quyền xem báo cáo hoạt động CLB."
            },
            HttpStatusCode.BadRequest => new ClubActivityReportApiResult
            {
                IsBadRequest = true,
                ErrorMessage = message ?? "Khoảng thời gian báo cáo không hợp lệ."
            },
            _ => new ClubActivityReportApiResult
            {
                ErrorMessage = message ?? "Không thể tải báo cáo hoạt động CLB."
            }
        };
    }

    private static string BuildQuery(
        string endpoint,
        DateTime? from,
        DateTime? to)
    {
        var query = new List<string>();
        if (from.HasValue)
        {
            query.Add($"from={Uri.EscapeDataString(from.Value.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))}");
        }

        if (to.HasValue)
        {
            query.Add($"to={Uri.EscapeDataString(to.Value.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))}");
        }

        return query.Count == 0
            ? endpoint
            : $"{endpoint}?{string.Join("&", query)}";
    }

    private static async Task<string?> ExtractMessageAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        try
        {
            using var document = JsonDocument.Parse(
                await response.Content.ReadAsStringAsync(cancellationToken));
            if (document.RootElement.ValueKind == JsonValueKind.Object &&
                document.RootElement.TryGetProperty("message", out var message))
            {
                return message.GetString();
            }

            return null;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
