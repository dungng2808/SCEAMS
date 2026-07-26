using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SCEAMS.MVC.Models.Api;
using SCEAMS.MVC.Services.ApiClients;
using SCEAMS.MVC.ViewModels;

namespace SCEAMS.MVC.Controllers;

[Route("Registrations")]
[Authorize(Roles = "Student")]
public sealed class RegistrationsController : Controller
{
    private readonly IRegistrationApiClient _registrationApiClient;
    private readonly ILogger<RegistrationsController> _logger;

    public RegistrationsController(
        IRegistrationApiClient registrationApiClient,
        ILogger<RegistrationsController> logger)
    {
        _registrationApiClient = registrationApiClient;
        _logger = logger;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(
        string? status,
        int page = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _registrationApiClient.GetMyRegistrationsAsync(
                status, page, pageSize, cancellationToken);
            if (result.IsUnauthorized && User.Identity?.IsAuthenticated == true)
            {
                return await EndInvalidSessionAsync(
                    result.ErrorMessage ?? "Phiên đăng nhập không còn hợp lệ.");
            }

            return View(new RegistrationHistoryViewModel
            {
                Status = status,
                Page = result.Page,
                PageSize = result.PageSize,
                TotalItems = result.TotalItems,
                TotalPages = result.TotalPages,
                HasPreviousPage = result.HasPreviousPage,
                HasNextPage = result.HasNextPage,
                ErrorMessage = result.IsSuccess ? null : result.ErrorMessage,
                Items = result.Items.Select(MapItem).ToList()
            });
        }
        catch (Exception exception) when (
            exception is HttpRequestException or TaskCanceledException or JsonException)
        {
            _logger.LogWarning(exception, "Unable to load Student registration history.");
            return View(new RegistrationHistoryViewModel
            {
                Status = status,
                Page = page,
                PageSize = pageSize,
                ErrorMessage = "Không thể kết nối tới API. Vui lòng thử lại sau."
            });
        }
    }

    private static RegistrationHistoryItemViewModel MapItem(
        RegistrationHistoryApiResponse item)
    {
        return new RegistrationHistoryItemViewModel
        {
            Id = item.Id,
            EventId = item.EventId,
            EventTitle = item.EventTitle,
            EventStatus = item.EventStatus,
            StartTime = item.StartTime,
            EndTime = item.EndTime,
            RegistrationStatus = item.RegistrationStatus,
            RegisteredAt = item.RegisteredAt,
            CancelledAt = item.CancelledAt,
            IsAttended = item.IsAttended,
            CheckInTime = item.CheckInTime
        };
    }

    private async Task<IActionResult> EndInvalidSessionAsync(string message)
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        HttpContext.Session.Clear();
        TempData["ErrorMessage"] = message;
        return RedirectToAction("Login", "Account", new { returnUrl = "/Registrations" });
    }
}
