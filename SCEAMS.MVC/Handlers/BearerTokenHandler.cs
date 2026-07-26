using System.Globalization;
using System.Net.Http.Headers;
using SCEAMS.MVC.Services.Authentication;

namespace SCEAMS.MVC.Handlers;

public sealed class BearerTokenHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly TimeProvider _timeProvider;

    public BearerTokenHandler(
        IHttpContextAccessor httpContextAccessor,
        TimeProvider timeProvider)
    {
        _httpContextAccessor = httpContextAccessor;
        _timeProvider = timeProvider;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var session = _httpContextAccessor.HttpContext?.Session;
        var accessToken = session?.GetString(SessionKeys.AccessToken);
        var expiresAtValue = session?.GetString(
            SessionKeys.AccessTokenExpiresAtUtc);

        if (!string.IsNullOrWhiteSpace(accessToken) &&
            DateTimeOffset.TryParse(
                expiresAtValue,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal,
                out var expiresAtUtc) &&
            expiresAtUtc > _timeProvider.GetUtcNow())
        {
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", accessToken);
        }

        return base.SendAsync(request, cancellationToken);
    }
}
