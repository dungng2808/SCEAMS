using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using SCEAMS.MVC.Models.Api;
using SCEAMS.MVC.Services.ApiClients;
using SCEAMS.MVC.ViewModels;

namespace SCEAMS.MVC.Controllers;

[Route("Events")]
public sealed class EventsController : Controller
{
    private readonly IEventApiClient _eventApiClient;
    private readonly ILogger<EventsController> _logger;

    public EventsController(
        IEventApiClient eventApiClient,
        ILogger<EventsController> logger)
    {
        _eventApiClient = eventApiClient;
        _logger = logger;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(
        string? search,
        int? clubId,
        DateTime? from,
        DateTime? to,
        string? status,
        bool? hasSlots,
        int page = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var normalizedPage = Math.Max(page, 1);
        var normalizedPageSize = Math.Clamp(pageSize, 1, 50);

        try
        {
            var result = await _eventApiClient.GetEventsAsync(
                search,
                clubId,
                from,
                to,
                status,
                hasSlots,
                normalizedPage,
                normalizedPageSize,
                cancellationToken);

            if (result.IsUnauthorized && User.Identity?.IsAuthenticated == true)
            {
                return await EndInvalidSessionAsync(
                    result.ErrorMessage ?? "Phiên đăng nhập không còn hợp lệ.");
            }

            var baseModel = new EventsViewModel
            {
                Search = search,
                ClubId = clubId,
                From = from,
                To = to,
                Status = status,
                HasSlots = hasSlots,
                Page = result.IsSuccess ? result.Page : normalizedPage,
                PageSize = result.IsSuccess ? result.PageSize : normalizedPageSize,
                TotalItems = result.TotalItems,
                TotalPages = result.TotalPages,
                HasPreviousPage = result.HasPreviousPage,
                HasNextPage = result.HasNextPage,
                ErrorMessage = result.IsSuccess
                    ? null
                    : result.ErrorMessage ?? "Không thể tải danh sách sự kiện.",
                Events = result.Events.Select(MapEvent).ToList()
            };

            return View(baseModel);
        }
        catch (Exception exception) when (
            exception is HttpRequestException or
            TaskCanceledException or
            JsonException)
        {
            _logger.LogWarning(exception, "Unable to load events from the API.");
            return View(new EventsViewModel
            {
                Search = search,
                ClubId = clubId,
                From = from,
                To = to,
                Status = status,
                HasSlots = hasSlots,
                Page = normalizedPage,
                PageSize = normalizedPageSize,
                ErrorMessage = "Không thể kết nối tới API. Vui lòng thử lại sau."
            });
        }
    }

    private static EventListItemViewModel MapEvent(EventApiResponse eventItem)
    {
        return new EventListItemViewModel
        {
            Id = eventItem.Id,
            Title = eventItem.Title,
            Status = eventItem.Status,
            ClubName = string.IsNullOrWhiteSpace(eventItem.ClubName)
                ? eventItem.Club.Name
                : eventItem.ClubName,
            VenueName = string.IsNullOrWhiteSpace(eventItem.VenueName)
                ? eventItem.Venue.Name
                : eventItem.VenueName,
            StartTime = eventItem.StartTime,
            EndTime = eventItem.EndTime,
            Capacity = eventItem.Capacity,
            RegisteredCount = eventItem.RegisteredCount,
            SlotsRemaining = eventItem.SlotsRemaining
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
