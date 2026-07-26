using System.Text.Json;
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

    private async Task<IActionResult> EndInvalidSessionAsync(
        string message)
    {
        HttpContext.Session.Clear();
        await HttpContext.SignOutAsync(
            CookieAuthenticationDefaults.AuthenticationScheme);

        TempData["AuthenticationError"] = message;

        return RedirectToAction(
            nameof(AccountController.Login),
            "Account",
            new { returnUrl = "/Profile" });
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
