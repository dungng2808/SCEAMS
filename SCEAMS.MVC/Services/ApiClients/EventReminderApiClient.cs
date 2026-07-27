using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using SCEAMS.MVC.Models;
using SCEAMS.MVC.Models.Api;

namespace SCEAMS.MVC.Services.ApiClients;

public sealed class EventReminderApiClient : IEventReminderApiClient
{
    private readonly HttpClient _httpClient;

    public EventReminderApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<EventReminderRunApiResult> RunAsync(
        CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.PostAsync(
            "api/reminders/run",
            content: null,
            cancellationToken);
        if (response.StatusCode == HttpStatusCode.OK)
        {
            var summary = await response.Content.ReadFromJsonAsync<
                EventReminderRunApiResponse>(
                cancellationToken: cancellationToken);
            return new EventReminderRunApiResult
            {
                IsSuccess = summary is not null,
                Summary = summary,
                ErrorMessage = summary is null ? "API trả về kết quả reminder không hợp lệ." : null
            };
        }

        var message = await ExtractMessageAsync(response, cancellationToken);
        return new EventReminderRunApiResult
        {
            IsUnauthorized = response.StatusCode == HttpStatusCode.Unauthorized,
            IsForbidden = response.StatusCode == HttpStatusCode.Forbidden,
            ErrorMessage = response.StatusCode == HttpStatusCode.Unauthorized
                ? "Phiên đăng nhập đã hết hạn hoặc không hợp lệ."
                : message ?? "Không thể chạy reminder job."
        };
    }

    private static async Task<string?> ExtractMessageAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        try
        {
            using var document = JsonDocument.Parse(
                await response.Content.ReadAsStringAsync(cancellationToken));
            return document.RootElement.TryGetProperty("message", out var message)
                ? message.GetString()
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
