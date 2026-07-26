using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using SCEAMS.MVC.Models.Api;
using SCEAMS.MVC.Services.ApiClients;
using SCEAMS.MVC.Services.Authentication;

namespace SCEAMS.MVC.Handlers;

public sealed class BearerTokenHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IAuthApiClient _authApiClient;
    private readonly RefreshTokenCoordinator _refreshCoordinator;
    private readonly ILogger<BearerTokenHandler> _logger;

    public BearerTokenHandler(
        IHttpContextAccessor httpContextAccessor,
        IAuthApiClient authApiClient,
        RefreshTokenCoordinator refreshCoordinator,
        ILogger<BearerTokenHandler> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _authApiClient = authApiClient;
        _refreshCoordinator = refreshCoordinator;
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        var accessToken = httpContext is null
            ? null
            : await GetStoredTokenAsync(
                httpContext,
                SessionKeys.AccessToken,
                AuthenticationTokenNames.AccessToken);
        HttpRequestMessage? retryRequest = null;

        if (!string.IsNullOrWhiteSpace(accessToken))
        {
            retryRequest = await CloneRequestAsync(
                request,
                cancellationToken);
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", accessToken);
        }

        var response = await base.SendAsync(
            request,
            cancellationToken);

        if (response.StatusCode != HttpStatusCode.Unauthorized ||
            httpContext is null ||
            retryRequest is null ||
            string.IsNullOrWhiteSpace(accessToken))
        {
            retryRequest?.Dispose();
            return response;
        }

        var refreshedAccessToken =
            await _refreshCoordinator.ExecuteAsync(
                () => TryRefreshAsync(
                    httpContext,
                    accessToken,
                    cancellationToken),
                cancellationToken);

        if (string.IsNullOrWhiteSpace(refreshedAccessToken))
        {
            retryRequest.Dispose();
            await ClearAuthenticationAsync(httpContext);
            return response;
        }

        response.Dispose();
        retryRequest.Headers.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                refreshedAccessToken);

        _logger.LogDebug(
            "Access token refreshed; retrying the API request once.");

        using (retryRequest)
        {
            return await base.SendAsync(
                retryRequest,
                cancellationToken);
        }
    }

    private async Task<string?> TryRefreshAsync(
        HttpContext httpContext,
        string failedAccessToken,
        CancellationToken cancellationToken)
    {
        var latestAccessToken = await GetStoredTokenAsync(
            httpContext,
            SessionKeys.AccessToken,
            AuthenticationTokenNames.AccessToken);

        if (!string.IsNullOrWhiteSpace(latestAccessToken) &&
            !string.Equals(
                latestAccessToken,
                failedAccessToken,
                StringComparison.Ordinal))
        {
            return latestAccessToken;
        }

        var refreshToken = await GetStoredTokenAsync(
            httpContext,
            SessionKeys.RefreshToken,
            AuthenticationTokenNames.RefreshToken);

        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return null;
        }

        RefreshTokenApiResult result;

        try
        {
            result = await _authApiClient.RefreshTokenAsync(
                new RefreshTokenApiRequest(refreshToken),
                cancellationToken);
        }
        catch (Exception exception) when (
            exception is HttpRequestException ||
            exception is TaskCanceledException &&
            !cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(
                exception,
                "Unable to refresh the access token.");
            return null;
        }

        if (!result.IsSuccess || result.Response is null)
        {
            return null;
        }

        await StoreRefreshedTokensAsync(
            httpContext,
            result.Response);

        return result.Response.AccessToken;
    }

    private static async Task<string?> GetStoredTokenAsync(
        HttpContext httpContext,
        string sessionKey,
        string authenticationTokenName)
    {
        var token = httpContext.Session.GetString(sessionKey);

        if (!string.IsNullOrWhiteSpace(token))
        {
            return token;
        }

        return await httpContext.GetTokenAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            authenticationTokenName);
    }

    private static async Task StoreRefreshedTokensAsync(
        HttpContext httpContext,
        RefreshTokenApiResponse response)
    {
        httpContext.Session.SetString(
            SessionKeys.AccessToken,
            response.AccessToken);
        httpContext.Session.SetString(
            SessionKeys.RefreshToken,
            response.RefreshToken);

        var authentication = await httpContext.AuthenticateAsync(
            CookieAuthenticationDefaults.AuthenticationScheme);

        if (!authentication.Succeeded ||
            authentication.Principal is null)
        {
            return;
        }

        var accessTokenExpiresAtUtc = new DateTimeOffset(
            response.ExpiresAtUtc.ToUniversalTime());
        var refreshTokenExpiresAtUtc = new DateTimeOffset(
            response.RefreshTokenExpiresAtUtc
                .ToUniversalTime());
        var properties = authentication.Properties ??
            new AuthenticationProperties();

        properties.AllowRefresh = false;
        properties.IsPersistent = false;
        properties.ExpiresUtc = refreshTokenExpiresAtUtc;
        properties.StoreTokens(
        [
            new AuthenticationToken
            {
                Name = AuthenticationTokenNames.AccessToken,
                Value = response.AccessToken
            },
            new AuthenticationToken
            {
                Name = AuthenticationTokenNames.RefreshToken,
                Value = response.RefreshToken
            },
            new AuthenticationToken
            {
                Name = AuthenticationTokenNames.TokenType,
                Value = response.TokenType
            },
            new AuthenticationToken
            {
                Name =
                    AuthenticationTokenNames
                        .AccessTokenExpiresAt,
                Value = accessTokenExpiresAtUtc.ToString(
                    "O",
                    CultureInfo.InvariantCulture)
            },
            new AuthenticationToken
            {
                Name =
                    AuthenticationTokenNames
                        .RefreshTokenExpiresAt,
                Value = refreshTokenExpiresAtUtc.ToString(
                    "O",
                    CultureInfo.InvariantCulture)
            }
        ]);

        await httpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            authentication.Principal,
            properties);
    }

    private static async Task ClearAuthenticationAsync(
        HttpContext httpContext)
    {
        httpContext.Session.Clear();
        await httpContext.SignOutAsync(
            CookieAuthenticationDefaults.AuthenticationScheme);
    }

    private static async Task<HttpRequestMessage>
        CloneRequestAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
    {
        var clone = new HttpRequestMessage(
            request.Method,
            request.RequestUri)
        {
            Version = request.Version,
            VersionPolicy = request.VersionPolicy
        };

        foreach (var header in request.Headers)
        {
            clone.Headers.TryAddWithoutValidation(
                header.Key,
                header.Value);
        }

        if (request.Content is not null)
        {
            var content = await request.Content
                .ReadAsByteArrayAsync(cancellationToken);
            clone.Content = new ByteArrayContent(content);

            foreach (var header in request.Content.Headers)
            {
                clone.Content.Headers.TryAddWithoutValidation(
                    header.Key,
                    header.Value);
            }
        }

        return clone;
    }
}
