using System.Net.Http.Headers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using SCEAMS.MVC.Services.Authentication;

namespace SCEAMS.MVC.Handlers;

public sealed class BearerTokenHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<BearerTokenHandler> _logger;

    public BearerTokenHandler(
        IHttpContextAccessor httpContextAccessor,
        ILogger<BearerTokenHandler> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        var session = httpContext?.Session;
        var accessToken = session?.GetString(SessionKeys.AccessToken);
        var hasSessionToken = !string.IsNullOrWhiteSpace(accessToken);
        var hasAuthenticationTicketToken = false;

        if (string.IsNullOrWhiteSpace(accessToken) &&
            httpContext is not null)
        {
            accessToken = await httpContext.GetTokenAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                "access_token");
            hasAuthenticationTicketToken =
                !string.IsNullOrWhiteSpace(accessToken);
        }

        if (!string.IsNullOrWhiteSpace(accessToken))
        {
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", accessToken);
        }

        _logger.LogDebug(
            "Bearer token lookup: HttpContext={HasHttpContext}, " +
            "Session={HasSessionToken}, AuthenticationTicket={HasAuthenticationTicketToken}.",
            httpContext is not null,
            hasSessionToken,
            hasAuthenticationTicketToken);

        return await base.SendAsync(request, cancellationToken);
    }
}
