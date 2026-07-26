using System.Net.Http.Headers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using SCEAMS.MVC.Services.Authentication;

namespace SCEAMS.MVC.Handlers;

public sealed class BearerTokenHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public BearerTokenHandler(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        var session = httpContext?.Session;
        var accessToken = session?.GetString(SessionKeys.AccessToken);

        if (string.IsNullOrWhiteSpace(accessToken) &&
            httpContext is not null)
        {
            accessToken = await httpContext.GetTokenAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                "access_token");
        }

        if (!string.IsNullOrWhiteSpace(accessToken))
        {
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", accessToken);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
