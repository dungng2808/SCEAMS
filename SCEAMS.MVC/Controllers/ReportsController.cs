using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SCEAMS.MVC.Services.ApiClients;
using SCEAMS.MVC.ViewModels;

namespace SCEAMS.MVC.Controllers;

[Route("Reports")]
[Authorize]
public sealed class ReportsController : Controller
{
    private readonly IReportApiClient _reportApiClient;
    private readonly ILogger<ReportsController> _logger;

    public ReportsController(
        IReportApiClient reportApiClient,
        ILogger<ReportsController> logger)
    {
        _reportApiClient = reportApiClient;
        _logger = logger;
    }

    [HttpGet("EventSummary")]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> EventSummary(
        DateTime? from,
        DateTime? to,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _reportApiClient.GetEventSummaryAsync(
                from,
                to,
                cancellationToken);
            if (result.IsUnauthorized && User.Identity?.IsAuthenticated == true)
            {
                return await EndInvalidSessionAsync(
                    result.ErrorMessage ?? "Phiên đăng nhập không còn hợp lệ.",
                    "/Reports/ClubActivity");
            }

            return View(new EventSummaryReportViewModel
            {
                From = from,
                To = to,
                TotalEvents = result.Report?.TotalEvents ?? 0,
                ErrorMessage = result.IsSuccess ? null : result.ErrorMessage,
                Items = result.Report?.Items
                    .Select(item => new EventSummaryReportItemViewModel
                    {
                        Status = item.Status,
                        Count = item.Count
                    })
                    .ToList() ?? []
            });
        }
        catch (Exception exception) when (
            exception is HttpRequestException or TaskCanceledException or JsonException)
        {
            _logger.LogWarning(exception, "Unable to load Event summary report.");
            return View(new EventSummaryReportViewModel
            {
                From = from,
                To = to,
                ErrorMessage = "Không thể kết nối tới API. Vui lòng thử lại sau."
            });
        }
    }

    [HttpGet("ClubActivity")]
    [Authorize(Roles = "Admin,Staff,Organizer")]
    public async Task<IActionResult> ClubActivity(
        DateTime? from,
        DateTime? to,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _reportApiClient.GetClubActivityAsync(
                from,
                to,
                cancellationToken);
            if (result.IsUnauthorized && User.Identity?.IsAuthenticated == true)
            {
                return await EndInvalidSessionAsync(
                    result.ErrorMessage ?? "Phiên đăng nhập không còn hợp lệ.");
            }

            return View(new ClubActivityReportViewModel
            {
                From = from,
                To = to,
                ErrorMessage = result.IsSuccess ? null : result.ErrorMessage,
                Items = result.Report?.Items
                    .Select(item => new ClubActivityReportItemViewModel
                    {
                        ClubId = item.ClubId,
                        ClubName = item.ClubName,
                        EventCount = item.EventCount,
                        RegistrationCount = item.RegistrationCount,
                        AttendanceCount = item.AttendanceCount,
                        AverageRating = item.AverageRating
                    })
                    .ToList() ?? []
            });
        }
        catch (Exception exception) when (
            exception is HttpRequestException or TaskCanceledException or JsonException)
        {
            _logger.LogWarning(exception, "Unable to load Club activity report.");
            return View(new ClubActivityReportViewModel
            {
                From = from,
                To = to,
                ErrorMessage = "Không thể kết nối tới API. Vui lòng thử lại sau."
            });
        }
    }

    [HttpGet("AttendanceRate")]
    [Authorize(Roles = "Admin,Staff,Organizer")]
    public async Task<IActionResult> AttendanceRate(
        DateTime? from,
        DateTime? to,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _reportApiClient.GetAttendanceRateAsync(
                from,
                to,
                cancellationToken);
            if (result.IsUnauthorized && User.Identity?.IsAuthenticated == true)
            {
                return await EndInvalidSessionAsync(
                    result.ErrorMessage ?? "Phiên đăng nhập không còn hợp lệ.",
                    "/Reports/AttendanceRate");
            }

            return View(new AttendanceRateReportViewModel
            {
                From = from,
                To = to,
                ErrorMessage = result.IsSuccess ? null : result.ErrorMessage,
                Items = result.Report?.Items
                    .Select(item => new AttendanceRateReportItemViewModel
                    {
                        EventId = item.EventId,
                        EventTitle = item.EventTitle,
                        ClubName = item.ClubName,
                        StartTime = item.StartTime,
                        Status = item.Status,
                        RegisteredCount = item.RegisteredCount,
                        AttendedCount = item.AttendedCount,
                        AttendanceRate = item.AttendanceRate
                    })
                    .ToList() ?? []
            });
        }
        catch (Exception exception) when (
            exception is HttpRequestException or TaskCanceledException or JsonException)
        {
            _logger.LogWarning(exception, "Unable to load attendance rate report.");
            return View(new AttendanceRateReportViewModel
            {
                From = from,
                To = to,
                ErrorMessage = "Không thể kết nối tới API. Vui lòng thử lại sau."
            });
        }
    }

    private async Task<IActionResult> EndInvalidSessionAsync(
        string message,
        string returnUrl = "/Reports/EventSummary")
    {
        await HttpContext.SignOutAsync(
            CookieAuthenticationDefaults.AuthenticationScheme);
        HttpContext.Session.Clear();
        TempData["ErrorMessage"] = message;
        return RedirectToAction(
            "Login",
            "Account",
            new { returnUrl });
    }
}
