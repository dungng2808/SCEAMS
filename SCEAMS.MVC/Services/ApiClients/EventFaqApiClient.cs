using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using SCEAMS.MVC.Models.Api;

namespace SCEAMS.MVC.Services.ApiClients;

public sealed class EventFaqApiClient : IEventFaqApiClient
{
    private readonly HttpClient _httpClient;

    public EventFaqApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<EventFaqRetrievalApiResult> RetrieveEventsAsync(
        EventFaqRetrievalApiRequest request,
        CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.PostAsJsonAsync(
            "api/chatbot/retrieval",
            request,
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.OK)
        {
            var payload = await response.Content.ReadFromJsonAsync<
                EventFaqRetrievalApiResponse>(cancellationToken: cancellationToken);
            return new EventFaqRetrievalApiResult
            {
                IsSuccess = payload is not null,
                StatusCode = (int)response.StatusCode,
                Events = payload?.RelatedEvents ?? [],
                ErrorMessage = payload is null
                    ? "API trả về dữ liệu retrieval không hợp lệ."
                    : null
            };
        }

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        return new EventFaqRetrievalApiResult
        {
            StatusCode = (int)response.StatusCode,
            ErrorMessage = ApiProblemDetailsParser.GetMessage(content) ??
                response.StatusCode switch
                {
                    HttpStatusCode.Unauthorized => "Phiên đăng nhập đã hết hạn hoặc không hợp lệ.",
                    HttpStatusCode.Forbidden => "Bạn không có quyền sử dụng trợ lý Event.",
                    HttpStatusCode.BadRequest => "Câu hỏi chưa hợp lệ. Hãy nhập ít nhất 2 ký tự.",
                    _ => "Không thể truy vấn Event lúc này. Vui lòng thử lại sau."
                }
        };
    }

    public async Task<AiChatApiResult> AskAsync(
        EventFaqRetrievalApiRequest request,
        CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.PostAsJsonAsync(
            "api/chatbot/ask",
            request,
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.OK)
        {
            var payload = await response.Content.ReadFromJsonAsync<AiChatApiResponse>(
                cancellationToken: cancellationToken);
            return new AiChatApiResult
            {
                IsSuccess = payload is not null,
                StatusCode = (int)response.StatusCode,
                Answer = payload?.Answer ?? string.Empty,
                Events = payload?.RelatedEvents ?? [],
                ErrorMessage = payload is null
                    ? "API trả về câu trả lời AI không hợp lệ."
                    : null
            };
        }

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        return new AiChatApiResult
        {
            StatusCode = (int)response.StatusCode,
            RetryAfterSeconds = response.Headers.RetryAfter?.Delta is { } delta
                ? Math.Max(1, (int)Math.Ceiling(delta.TotalSeconds))
                : null,
            ErrorMessage = ApiProblemDetailsParser.GetMessage(content) ??
                response.StatusCode switch
                {
                    HttpStatusCode.Unauthorized => "Phiên đăng nhập đã hết hạn hoặc không hợp lệ.",
                    HttpStatusCode.Forbidden => "Bạn không có quyền sử dụng trợ lý Event.",
                    HttpStatusCode.ServiceUnavailable => "AI provider hiện không khả dụng. Hãy thử lại sau.",
                    _ => "Không thể nhận câu trả lời lúc này. Vui lòng thử lại sau."
                }
        };
    }

    public async Task<ChatHistoryApiResult> GetHistoryAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var normalizedPage = Math.Max(page, 1);
        var normalizedPageSize = Math.Clamp(pageSize, 1, 50);
        using var response = await _httpClient.GetAsync(
            $"api/chatbot/history?page={normalizedPage}&pageSize={normalizedPageSize}",
            cancellationToken);
        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        if (response.StatusCode == HttpStatusCode.OK)
        {
            var payload = JsonSerializer.Deserialize<ChatHistoryPageApiResponse>(
                content,
                new JsonSerializerOptions(JsonSerializerDefaults.Web));
            return new ChatHistoryApiResult
            {
                IsSuccess = payload is not null,
                StatusCode = (int)response.StatusCode,
                Page = payload,
                ErrorMessage = payload is null
                    ? "API trả về lịch sử chatbot không hợp lệ."
                    : null
            };
        }

        return new ChatHistoryApiResult
        {
            StatusCode = (int)response.StatusCode,
            ErrorMessage = ApiProblemDetailsParser.GetMessage(content) ??
                response.StatusCode switch
                {
                    HttpStatusCode.Unauthorized => "Phiên đăng nhập đã hết hạn hoặc không hợp lệ.",
                    HttpStatusCode.Forbidden => "Bạn không có quyền xem lịch sử chatbot.",
                    _ => "Không thể tải lịch sử chatbot lúc này."
                }
        };
    }
}
