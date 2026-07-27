using System.Net.Http.Headers;
using SCEAMS.MVC.Models;

namespace SCEAMS.MVC.Services.ApiClients;

public sealed class ContentNegotiationApiClient : IContentNegotiationApiClient
{
    private readonly HttpClient _httpClient;

    public ContentNegotiationApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<ContentNegotiationApiResult> GetEventsAsync(
        string acceptMediaType,
        int top,
        CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"api/events?$top={Math.Clamp(top, 1, 50)}");
        request.Headers.Accept.Add(
            new MediaTypeWithQualityHeaderValue(acceptMediaType));

        using var response = await _httpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);
        return new ContentNegotiationApiResult
        {
            StatusCode = (int)response.StatusCode,
            StatusDescription = response.ReasonPhrase ?? response.StatusCode.ToString(),
            ContentType = response.Content.Headers.ContentType?.ToString() ?? "(không có)",
            RawResponse = await response.Content.ReadAsStringAsync(cancellationToken)
        };
    }
}
