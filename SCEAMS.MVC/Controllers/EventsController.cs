using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using SCEAMS.MVC.Models.Api;
using SCEAMS.MVC.Services.ApiClients;
using SCEAMS.MVC.ViewModels;

namespace SCEAMS.MVC.Controllers;

[Route("Events")]
public sealed class EventsController : Controller
{
    private readonly IEventApiClient _eventApiClient;
    private readonly IClubApiClient _clubApiClient;
    private readonly IVenueApiClient _venueApiClient;
    private readonly ILogger<EventsController> _logger;

    public EventsController(
        IEventApiClient eventApiClient,
        IClubApiClient clubApiClient,
        IVenueApiClient venueApiClient,
        ILogger<EventsController> logger)
    {
        _eventApiClient = eventApiClient;
        _clubApiClient = clubApiClient;
        _venueApiClient = venueApiClient;
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

    [Authorize(Roles = "Organizer")]
    [HttpGet("Create")]
    public async Task<IActionResult> Create(CancellationToken cancellationToken = default)
    {
        var model = new CreateEventViewModel
        {
            StartTime = DateTime.Now.AddDays(7).AddHours(1),
            EndTime = DateTime.Now.AddDays(7).AddHours(3),
            RegistrationDeadline = DateTime.Now.AddDays(5)
        };

        var optionsResult = await LoadCreateOptionsAsync(model, cancellationToken);
        if (optionsResult.IsUnauthorized)
        {
            return await EndInvalidSessionAsync(
                optionsResult.ErrorMessage ?? "Phiên đăng nhập không còn hợp lệ.");
        }

        if (!optionsResult.IsSuccess)
        {
            model.ErrorMessage = optionsResult.ErrorMessage;
        }

        return View(model);
    }

    [Authorize(Roles = "Organizer")]
    [HttpPost("Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        CreateEventViewModel model,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            await LoadCreateOptionsAsync(model, cancellationToken);
            return View(model);
        }

        try
        {
            var result = await _eventApiClient.CreateEventAsync(
                new CreateEventApiRequest(
                    model.Title.Trim(),
                    string.IsNullOrWhiteSpace(model.Description)
                        ? null
                        : model.Description.Trim(),
                    model.ClubId,
                    model.VenueId,
                    model.StartTime,
                    model.EndTime,
                    model.RegistrationDeadline,
                    model.Capacity),
                cancellationToken);

            if (result.IsUnauthorized && User.Identity?.IsAuthenticated == true)
            {
                return await EndInvalidSessionAsync(
                    result.ErrorMessage ?? "Phiên đăng nhập không còn hợp lệ.");
            }

            if (result.IsForbidden)
            {
                return RedirectToAction(nameof(AccountController.AccessDenied), "Account");
            }

            if (result.IsConflict || result.IsNotFound || !result.IsSuccess || result.Event is null)
            {
                ModelState.AddModelError(
                    string.Empty,
                    result.ErrorMessage ?? "Không thể tạo Event Draft.");
                model.ErrorMessage = result.ErrorMessage;
                await LoadCreateOptionsAsync(model, cancellationToken);
                return View(model);
            }

            TempData["SuccessMessage"] = $"Đã tạo Event Draft '{result.Event.Title}'.";
            return RedirectToAction(nameof(Detail), new { id = result.Event.Id });
        }
        catch (Exception exception) when (
            exception is HttpRequestException or
            TaskCanceledException or
            JsonException)
        {
            _logger.LogWarning(exception, "Unable to create event from MVC.");
            model.ErrorMessage = "Không thể kết nối tới API. Vui lòng thử lại sau.";
            await LoadCreateOptionsAsync(model, cancellationToken);
            return View(model);
        }
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Detail(
        int id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _eventApiClient.GetEventByIdAsync(
                id,
                cancellationToken);

            if (result.IsUnauthorized && User.Identity?.IsAuthenticated == true)
            {
                return await EndInvalidSessionAsync(
                    result.ErrorMessage ?? "Phiên đăng nhập không còn hợp lệ.");
            }

            if (!result.IsSuccess || result.Event is null)
            {
                if (result.IsNotFound)
                {
                    Response.StatusCode = StatusCodes.Status404NotFound;
                }

                return View(new EventDetailViewModel
                {
                    Id = id,
                    IsNotFound = result.IsNotFound,
                    ErrorMessage = result.ErrorMessage ??
                        "Không thể tải chi tiết Event."
                });
            }

            return View(MapDetail(result.Event));
        }
        catch (Exception exception) when (
            exception is HttpRequestException or
            TaskCanceledException or
            JsonException)
        {
            _logger.LogWarning(exception, "Unable to load event {EventId}.", id);
            return View(new EventDetailViewModel
            {
                Id = id,
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

    private async Task<CreateOptionsResult> LoadCreateOptionsAsync(
        CreateEventViewModel model,
        CancellationToken cancellationToken)
    {
        var clubsResult = await _clubApiClient.GetClubsAsync(
            new ClubListApiQuery(
                Status: "Approved",
                OrderBy: "Name asc",
                Page: 1,
                PageSize: 50),
            cancellationToken);
        if (clubsResult.IsUnauthorized)
        {
            return new CreateOptionsResult(false, true, clubsResult.ErrorMessage);
        }

        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var hasUserId = int.TryParse(userIdValue, out var userId);
        model.Clubs = clubsResult.IsSuccess
            ? clubsResult.Clubs
                .Where(club => !hasUserId || club.CreatedByUserId == userId)
                .ToList()
            : [];

        var venuesResult = await _venueApiClient.GetVenuesAsync(
            search: null,
            maintenance: false,
            page: 1,
            pageSize: 50,
            cancellationToken);
        if (venuesResult.IsUnauthorized)
        {
            return new CreateOptionsResult(false, true, venuesResult.ErrorMessage);
        }

        model.Venues = venuesResult.IsSuccess
            ? venuesResult.Venues
            : [];

        if (!clubsResult.IsSuccess || !venuesResult.IsSuccess)
        {
            return new CreateOptionsResult(
                false,
                false,
                clubsResult.ErrorMessage ?? venuesResult.ErrorMessage ??
                    "Không thể tải danh sách Club/Venue.");
        }

        return new CreateOptionsResult(true, false, null);
    }

    private sealed record CreateOptionsResult(
        bool IsSuccess,
        bool IsUnauthorized,
        string? ErrorMessage);

    private static EventDetailViewModel MapDetail(EventDetailApiResponse eventItem)
    {
        return new EventDetailViewModel
        {
            Id = eventItem.Id,
            Title = eventItem.Title,
            Status = eventItem.Status,
            Description = eventItem.Description,
            ClubName = eventItem.ClubName,
            VenueName = eventItem.VenueName,
            VenueLocation = eventItem.VenueLocation,
            StartTime = eventItem.StartTime,
            EndTime = eventItem.EndTime,
            RegistrationDeadline = eventItem.RegistrationDeadline,
            Capacity = eventItem.Capacity,
            RegisteredCount = eventItem.RegisteredCount,
            SlotsRemaining = eventItem.SlotsRemaining,
            CreatedByUserName = eventItem.CreatedByUserName,
            RejectionReason = eventItem.RejectionReason,
            CancellationReason = eventItem.CancellationReason,
            Permissions = new EventPermissionsViewModel
            {
                CanEdit = eventItem.Permissions.CanEdit,
                CanSubmit = eventItem.Permissions.CanSubmit,
                CanApprove = eventItem.Permissions.CanApprove,
                CanReject = eventItem.Permissions.CanReject,
                CanCancel = eventItem.Permissions.CanCancel,
                CanRegister = eventItem.Permissions.CanRegister
            }
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
