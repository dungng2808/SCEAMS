using System.Text.Json;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SCEAMS.MVC.Models.Api;
using SCEAMS.MVC.Services.ApiClients;
using SCEAMS.MVC.ViewModels;

namespace SCEAMS.MVC.Controllers;

[Authorize]
[Route("Profile")]
public sealed class ProfileController : Controller
{
    private static readonly TimeZoneInfo BusinessTimeZone =
        ResolveBusinessTimeZone();

    private readonly IUserApiClient _userApiClient;
    private readonly ILogger<ProfileController> _logger;

    public ProfileController(
        IUserApiClient userApiClient,
        ILogger<ProfileController> logger)
    {
        _userApiClient = userApiClient;
        _logger = logger;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _userApiClient.GetCurrentUserAsync(
                cancellationToken);

            if (result.IsUnauthorized || result.IsNotFound)
            {
                return await EndInvalidSessionAsync(
                    result.ErrorMessage ??
                    "Phiên đăng nhập không còn hợp lệ.");
            }

            if (!result.IsSuccess || result.Profile is null)
            {
                return View(new ProfileViewModel
                {
                    ErrorMessage = result.ErrorMessage ??
                        "Không thể tải hồ sơ vào lúc này."
                });
            }

            return View(new ProfileViewModel
            {
                Profile = MapProfile(result.Profile)
            });
        }
        catch (Exception exception) when (
            exception is HttpRequestException or
            TaskCanceledException or
            JsonException)
        {
            _logger.LogWarning(
                exception,
                "Unable to load the current SCEAMS profile.");

            return View(new ProfileViewModel
            {
                ErrorMessage =
                    "Không thể kết nối tới API để tải hồ sơ. Vui lòng thử lại."
            });
        }
    }

    [HttpGet("Edit")]
    public async Task<IActionResult> Edit(
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _userApiClient.GetCurrentUserAsync(
                cancellationToken);

            if (result.IsUnauthorized || result.IsNotFound)
            {
                return await EndInvalidSessionAsync(
                    result.ErrorMessage ??
                    "Phiên đăng nhập không còn hợp lệ.",
                    "/Profile/Edit");
            }

            if (!result.IsSuccess || result.Profile is null)
            {
                return View(new EditProfileViewModel
                {
                    LoadErrorMessage = result.ErrorMessage ??
                        "Không thể tải hồ sơ vào lúc này."
                });
            }

            return View(new EditProfileViewModel
            {
                FullName = result.Profile.FullName,
                PhoneNumber = result.Profile.PhoneNumber
            });
        }
        catch (Exception exception) when (
            exception is HttpRequestException or
            TaskCanceledException or
            JsonException)
        {
            _logger.LogWarning(
                exception,
                "Unable to load the profile edit form.");

            return View(new EditProfileViewModel
            {
                LoadErrorMessage =
                    "Không thể kết nối tới API để tải hồ sơ. Vui lòng thử lại."
            });
        }
    }

    [HttpPost("Edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        EditProfileViewModel model,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var result = await _userApiClient.UpdateCurrentUserAsync(
                new UpdateCurrentUserProfileApiRequest(
                    FullName: model.FullName,
                    PhoneNumber: model.PhoneNumber),
                cancellationToken);

            if (result.IsUnauthorized || result.IsNotFound)
            {
                return await EndInvalidSessionAsync(
                    result.ErrorMessage ??
                    "Phiên đăng nhập không còn hợp lệ.",
                    "/Profile/Edit");
            }

            if (result.IsSuccess && result.Profile is not null)
            {
                await RefreshDisplayNameAsync(
                    result.Profile.FullName);

                TempData["ProfileUpdateSuccess"] =
                    "Hồ sơ đã được cập nhật và tải lại từ API.";

                return RedirectToAction(nameof(Index));
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
            TaskCanceledException or
            JsonException)
        {
            _logger.LogWarning(
                exception,
                "Unable to update the current SCEAMS profile.");

            ModelState.AddModelError(
                string.Empty,
                "Không thể kết nối tới API. Vui lòng thử lại sau.");
        }

        return View(model);
    }

    private async Task<IActionResult> EndInvalidSessionAsync(
        string message,
        string returnUrl = "/Profile")
    {
        HttpContext.Session.Clear();
        await HttpContext.SignOutAsync(
            CookieAuthenticationDefaults.AuthenticationScheme);

        TempData["AuthenticationError"] = message;

        return RedirectToAction(
            nameof(AccountController.Login),
            "Account",
            new { returnUrl });
    }

    private async Task RefreshDisplayNameAsync(string fullName)
    {
        var authentication = await HttpContext.AuthenticateAsync(
            CookieAuthenticationDefaults.AuthenticationScheme);

        if (!authentication.Succeeded ||
            authentication.Principal is null)
        {
            return;
        }

        var claims = authentication.Principal.Claims
            .Where(claim => claim.Type != ClaimTypes.Name)
            .Append(new Claim(ClaimTypes.Name, fullName));
        var identity = new ClaimsIdentity(
            claims,
            CookieAuthenticationDefaults.AuthenticationScheme,
            ClaimTypes.Name,
            ClaimTypes.Role);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            authentication.Properties ??
                new AuthenticationProperties());
    }

    private static ProfileDetailsViewModel MapProfile(
        CurrentUserProfileApiResponse profile)
    {
        var createdAtUtc = DateTime.SpecifyKind(
            profile.CreatedAt,
            DateTimeKind.Utc);
        var createdAtLocal = TimeZoneInfo.ConvertTime(
            new DateTimeOffset(createdAtUtc),
            BusinessTimeZone);

        return new ProfileDetailsViewModel
        {
            Id = profile.Id,
            Initials = GetInitials(profile.FullName),
            FullName = profile.FullName,
            Email = profile.Email,
            StudentCode = profile.StudentCode,
            PhoneNumber = profile.PhoneNumber,
            Role = profile.Role,
            IsActive = profile.IsActive,
            CreatedAtUtc = createdAtUtc,
            CreatedAtLocal = createdAtLocal
        };
    }

    private static string GetInitials(string fullName)
    {
        var parts = fullName.Split(
            ' ',
            StringSplitOptions.RemoveEmptyEntries |
            StringSplitOptions.TrimEntries);

        if (parts.Length == 0)
        {
            return "SC";
        }

        var initials = parts.Length == 1
            ? parts[0][..1]
            : $"{parts[0][0]}{parts[^1][0]}";

        return initials.ToUpperInvariant();
    }

    private static TimeZoneInfo ResolveBusinessTimeZone()
    {
        foreach (var timeZoneId in new[]
        {
            "Asia/Ho_Chi_Minh",
            "SE Asia Standard Time"
        })
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(
                    timeZoneId);
            }
            catch (TimeZoneNotFoundException)
            {
            }
            catch (InvalidTimeZoneException)
            {
            }
        }

        return TimeZoneInfo.CreateCustomTimeZone(
            "SCEAMS Business Time",
            TimeSpan.FromHours(7),
            "SCEAMS Business Time",
            "SCEAMS Business Time");
    }
}
