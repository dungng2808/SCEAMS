using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using SCEAMS.MVC.Models;
using SCEAMS.MVC.Models.Api;

namespace SCEAMS.MVC.Services.ApiClients;

public sealed class NotificationLogApiClient : INotificationLogApiClient
{
    private readonly HttpClient _httpClient;

    public NotificationLogApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<NotificationLogApiResult> GetLogsAsync(
        int? eventId,
        string? notificationType,
        bool? success,
        CancellationToken cancellationToken = default)
    {
        var query = new List<string>();
        if (eventId.HasValue && eventId.Value > 0)
        {
            query.Add($"eventId={eventId.Value}");
        }

        if (!string.IsNullOrWhiteSpace(notificationType))
        {
            query.Add($"notificationType={Uri.EscapeDataString(notificationType.Trim())}");
        }

        if (success.HasValue)
        {
            query.Add($"success={success.Value.ToString().ToLowerInvariant()}");
        }

        var endpoint = "api/notifications/logs?limit=100" +
            (query.Count == 0 ? string.Empty : "&" + string.Join("&", query));
        using var response = await _httpClient.GetAsync(endpoint, cancellationToken);
        if (response.StatusCode == HttpStatusCode.OK)
        {
            var entries = await response.Content.ReadFromJsonAsync<
                IReadOnlyList<NotificationLogApiResponse>>(
                cancellationToken: cancellationToken);
            return new NotificationLogApiResult
            {
                IsSuccess = entries is not null,
                Entries = entries ?? [],
                ErrorMessage = entries is null ? "API trả về log notification không hợp lệ." : null
            };
        }

        var message = await ExtractMessageAsync(response, cancellationToken);
        return new NotificationLogApiResult
        {
            IsUnauthorized = response.StatusCode == HttpStatusCode.Unauthorized,
            IsForbidden = response.StatusCode == HttpStatusCode.Forbidden,
            ErrorMessage = response.StatusCode == HttpStatusCode.Unauthorized
                ? "Phiên đăng nhập đã hết hạn hoặc không hợp lệ."
                : message ?? "Không thể tải notification log."
        };
    }

    private static async Task<string?> ExtractMessageAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        try
        {
            return ApiProblemDetailsParser.GetMessage(
                await response.Content.ReadAsStringAsync(cancellationToken));
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
