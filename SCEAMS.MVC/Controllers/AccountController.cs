using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SCEAMS.MVC.Models.Api;
using SCEAMS.MVC.Services.ApiClients;
using SCEAMS.MVC.Services.Authentication;
using SCEAMS.MVC.ViewModels;

namespace SCEAMS.MVC.Controllers;

[Route("Account")]
public sealed class AccountController : Controller
{
    private readonly IAuthApiClient _authApiClient;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        IAuthApiClient authApiClient,
        ILogger<AccountController> logger)
    {
        _authApiClient = authApiClient;
        _logger = logger;
    }

    [HttpGet("Register")]
    public IActionResult Register()
    {
        return View(new RegisterViewModel());
    }

    [HttpPost("Register")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(
        RegisterViewModel model,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var request = new RegisterStudentApiRequest(
                FullName: model.FullName,
                Email: model.Email,
                StudentCode: model.StudentCode,
                PhoneNumber: model.PhoneNumber,
                Password: model.Password,
                ConfirmPassword: model.ConfirmPassword);

            var result = await _authApiClient.RegisterStudentAsync(
                request,
                cancellationToken);

            if (result.IsSuccess && result.Student is not null)
            {
                TempData["RegistrationSuccess"] =
                    $"Đăng ký thành công cho {result.Student.Email}. Bạn có thể đăng nhập.";

                return RedirectToAction(nameof(Login));
            }

            foreach (var fieldError in result.FieldErrors)
            {
                foreach (var message in fieldError.Value)
                {
                    ModelState.AddModelError(
                        fieldError.Key,
                        message);
                }
            }

            if (result.ErrorMessage is not null)
            {
                ModelState.AddModelError(
                    string.Empty,
                    result.ErrorMessage);
            }
        }
        catch (Exception exception) when (
            exception is HttpRequestException or
            TaskCanceledException)
        {
            _logger.LogWarning(
                exception,
                "Unable to register the Student through SCEAMS API.");

            ModelState.AddModelError(
                string.Empty,
                "Không thể kết nối tới API. Vui lòng thử lại sau.");
        }

        return View(model);
    }

    [HttpGet("Login")]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction(
                nameof(DashboardController.Index),
                "Dashboard");
        }

        return View(new LoginViewModel
        {
            ReturnUrl = returnUrl
        });
    }

    [HttpPost("Login")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(
        LoginViewModel model,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var result = await _authApiClient.LoginAsync(
                new LoginApiRequest(
                    Email: model.Email,
                    Password: model.Password),
                cancellationToken);

            if (result.IsSuccess && result.Response is not null)
            {
                await EstablishAuthenticatedSessionAsync(
                    result.Response);

                if (Url.IsLocalUrl(model.ReturnUrl))
                {
                    return LocalRedirect(model.ReturnUrl);
                }

                return RedirectToAction(
                    nameof(DashboardController.RoleDashboard),
                    "Dashboard",
                    new { role = result.Response.User.Role });
            }

            foreach (var fieldError in result.FieldErrors)
            {
                foreach (var message in fieldError.Value)
                {
                    ModelState.AddModelError(
                        fieldError.Key,
                        message);
                }
            }

            if (result.ErrorMessage is not null)
            {
                ModelState.AddModelError(
                    string.Empty,
                    result.ErrorMessage);
            }
        }
        catch (Exception exception) when (
            exception is HttpRequestException or
            TaskCanceledException)
        {
            _logger.LogWarning(
                exception,
                "Unable to log in through SCEAMS API.");

            ModelState.AddModelError(
                string.Empty,
                "Không thể kết nối tới API. Vui lòng thử lại sau.");
        }

        return View(model);
    }

    [Authorize]
    [HttpPost("Logout")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        var refreshToken = HttpContext.Session.GetString(
            SessionKeys.RefreshToken);

        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            refreshToken = await HttpContext.GetTokenAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                AuthenticationTokenNames.RefreshToken);
        }

        if (!string.IsNullOrWhiteSpace(refreshToken))
        {
            try
            {
                await _authApiClient.RevokeTokenAsync(
                    new RefreshTokenApiRequest(refreshToken),
                    HttpContext.RequestAborted);
            }
            catch (Exception exception) when (
                exception is HttpRequestException or
                TaskCanceledException)
            {
                _logger.LogWarning(
                    exception,
                    "Unable to revoke the refresh token during logout.");
            }
        }

        HttpContext.Session.Clear();
        await HttpContext.SignOutAsync(
            CookieAuthenticationDefaults.AuthenticationScheme);

        TempData["LogoutSuccess"] = "Bạn đã đăng xuất an toàn.";

        return RedirectToAction(nameof(Login));
    }

    [HttpGet("AccessDenied")]
    public IActionResult AccessDenied()
    {
        Response.StatusCode = StatusCodes.Status403Forbidden;

        return View();
    }

    private async Task EstablishAuthenticatedSessionAsync(
        LoginApiResponse response)
    {
        HttpContext.Session.Clear();
        HttpContext.Session.SetString(
            SessionKeys.AccessToken,
            response.AccessToken);
        HttpContext.Session.SetString(
            SessionKeys.RefreshToken,
            response.RefreshToken);

        var claims = new[]
        {
            new Claim(
                ClaimTypes.NameIdentifier,
                response.User.Id.ToString(
                    CultureInfo.InvariantCulture)),
            new Claim(
                ClaimTypes.Name,
                response.User.FullName),
            new Claim(
                ClaimTypes.Email,
                response.User.Email),
            new Claim(
                ClaimTypes.Role,
                response.User.Role)
        };
        var identity = new ClaimsIdentity(
            claims,
            CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        var accessTokenExpiresAtUtc = new DateTimeOffset(
            response.ExpiresAtUtc.ToUniversalTime());
        var refreshTokenExpiresAtUtc = new DateTimeOffset(
            response.RefreshTokenExpiresAtUtc
                .ToUniversalTime());
        var authenticationProperties = new AuthenticationProperties
        {
            AllowRefresh = false,
            IsPersistent = false,
            ExpiresUtc = refreshTokenExpiresAtUtc
        };
        authenticationProperties.StoreTokens(
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

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            authenticationProperties);
    }
}
