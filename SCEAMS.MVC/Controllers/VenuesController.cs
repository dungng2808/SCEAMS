using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using SCEAMS.MVC.Models.Api;
using SCEAMS.MVC.Services.ApiClients;
using SCEAMS.MVC.ViewModels;

namespace SCEAMS.MVC.Controllers;

[Route("Venues")]
public sealed class VenuesController : Controller
{
    private readonly IVenueApiClient _venueApiClient;
    private readonly ILogger<VenuesController> _logger;

    public VenuesController(
        IVenueApiClient venueApiClient,
        ILogger<VenuesController> logger)
    {
        _venueApiClient = venueApiClient;
        _logger = logger;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(
        string? search,
        bool? maintenance,
        int page = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var canManage = User.IsInRole("Admin") || User.IsInRole("Staff");
        var normalizedPage = Math.Max(page, 1);
        var normalizedPageSize = Math.Clamp(pageSize, 1, 50);

        try
        {
            var result = await _venueApiClient.GetVenuesAsync(
                search,
                maintenance,
                normalizedPage,
                normalizedPageSize,
                cancellationToken);

            if (result.IsUnauthorized && User.Identity?.IsAuthenticated == true)
            {
                return await EndInvalidSessionAsync(
                    result.ErrorMessage ?? "Phiên đăng nhập không còn hợp lệ.");
            }

            if (!result.IsSuccess)
            {
                return View(new VenuesViewModel
                {
                    Search = search,
                    Maintenance = maintenance,
                    Page = normalizedPage,
                    PageSize = normalizedPageSize,
                    CanManage = canManage,
                    ErrorMessage = result.ErrorMessage ?? "Không thể tải danh sách địa điểm."
                });
            }

            return View(new VenuesViewModel
            {
                Search = search,
                Maintenance = maintenance,
                Page = result.Page,
                PageSize = result.PageSize,
                TotalItems = result.TotalItems,
                TotalPages = result.TotalPages,
                HasPreviousPage = result.HasPreviousPage,
                HasNextPage = result.HasNextPage,
                CanManage = canManage,
                Venues = result.Venues.Select(MapVenue).ToList()
            });
        }
        catch (Exception exception) when (
            exception is HttpRequestException or
            TaskCanceledException or
            JsonException)
        {
            _logger.LogWarning(exception, "Unable to load venues from the API.");

            return View(new VenuesViewModel
            {
                Search = search,
                Maintenance = maintenance,
                Page = normalizedPage,
                PageSize = normalizedPageSize,
                CanManage = canManage,
                ErrorMessage = "Không thể kết nối tới API. Vui lòng thử lại sau."
            });
        }
    }

    private static VenueListItemViewModel MapVenue(VenueApiResponse venue)
    {
        return new VenueListItemViewModel
        {
            Id = venue.Id,
            Name = venue.Name,
            Location = venue.Location,
            Capacity = venue.Capacity,
            IsUnderMaintenance = venue.IsUnderMaintenance
        };
    }

    private async Task<IActionResult> EndInvalidSessionAsync(string message)
    {
        var returnUrl = $"{Request.PathBase}{Request.Path}{Request.QueryString}";

        HttpContext.Session.Clear();
        await HttpContext.SignOutAsync(
            CookieAuthenticationDefaults.AuthenticationScheme);

        TempData["AuthenticationError"] = message;
        return RedirectToAction(
            nameof(AccountController.Login),
            "Account",
            new { returnUrl });
    }
}
