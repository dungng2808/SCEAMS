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

    [Authorize(Roles = "Admin,Staff")]
    [HttpGet("Pending")]
    public async Task<IActionResult> Pending(
        int? clubId,
        int? venueId,
        DateTime? from,
        DateTime? to,
        int page = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var normalizedPage = Math.Max(page, 1);
        var normalizedPageSize = Math.Clamp(pageSize, 1, 50);
        try
        {
            var result = await _eventApiClient.GetPendingApprovalEventsAsync(
                clubId,
                venueId,
                from,
                to,
                normalizedPage,
                normalizedPageSize,
                cancellationToken);
            if (result.IsUnauthorized && User.Identity?.IsAuthenticated == true)
            {
                return await EndInvalidSessionAsync(
                    result.ErrorMessage ?? "Phiên đăng nhập không còn hợp lệ.");
            }

            return View(new PendingEventsViewModel
            {
                ClubId = clubId,
                VenueId = venueId,
                From = from,
                To = to,
                Page = result.IsSuccess ? result.Page : normalizedPage,
                PageSize = result.IsSuccess ? result.PageSize : normalizedPageSize,
                TotalItems = result.TotalItems,
                TotalPages = result.TotalPages,
                HasPreviousPage = result.HasPreviousPage,
                HasNextPage = result.HasNextPage,
                ErrorMessage = result.IsSuccess ? null : result.ErrorMessage,
                Events = result.Events.Select(MapEvent).ToList()
            });
        }
        catch (Exception exception) when (
            exception is HttpRequestException or
            TaskCanceledException or
            JsonException)
        {
            _logger.LogWarning(exception, "Unable to load pending event queue.");
            return View(new PendingEventsViewModel
            {
                ClubId = clubId,
                VenueId = venueId,
                From = from,
                To = to,
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

    [Authorize(Roles = "Organizer,Admin,Staff")]
    [HttpGet("{id:int}/Edit")]
    public async Task<IActionResult> Edit(
        int id,
        CancellationToken cancellationToken = default)
    {
        var detail = await _eventApiClient.GetEventByIdAsync(id, cancellationToken);
        if (detail.IsUnauthorized && User.Identity?.IsAuthenticated == true)
        {
            return await EndInvalidSessionAsync(
                detail.ErrorMessage ?? "Phiên đăng nhập không còn hợp lệ.");
        }

        if (!detail.IsSuccess || detail.Event is null)
        {
            if (detail.IsNotFound)
            {
                Response.StatusCode = StatusCodes.Status404NotFound;
            }

            return View(new EditEventViewModel
            {
                Id = id,
                IsNotFound = detail.IsNotFound,
                LoadErrorMessage = detail.ErrorMessage ?? "Không thể tải Event để sửa."
            });
        }

        if (!detail.Event.Permissions.CanEdit)
        {
            Response.StatusCode = StatusCodes.Status403Forbidden;
            return View(new EditEventViewModel
            {
                Id = id,
                IsNotFound = true,
                LoadErrorMessage = "Bạn không có quyền sửa Event ở trạng thái hiện tại."
            });
        }

        var model = MapEdit(detail.Event);
        var options = await LoadEditOptionsAsync(model, cancellationToken);
        if (options.IsUnauthorized)
        {
            return await EndInvalidSessionAsync(
                options.ErrorMessage ?? "Phiên đăng nhập không còn hợp lệ.");
        }

        if (!options.IsSuccess)
        {
            model.ErrorMessage = options.ErrorMessage;
        }

        return View(model);
    }

    [Authorize(Roles = "Organizer,Admin,Staff")]
    [HttpPost("{id:int}/Edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int id,
        EditEventViewModel model,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            await LoadEditOptionsAsync(model, cancellationToken);
            return View(model);
        }

        try
        {
            var result = await _eventApiClient.UpdateEventAsync(
                id,
                new UpdateEventApiRequest(
                    model.Title.Trim(),
                    string.IsNullOrWhiteSpace(model.Description)
                        ? null
                        : model.Description.Trim(),
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

            if (result.IsNotFound)
            {
                Response.StatusCode = StatusCodes.Status404NotFound;
                model.ErrorMessage = result.ErrorMessage ?? "Event không tồn tại.";
                return View(model);
            }

            if (result.IsConflict || !result.IsSuccess || result.Event is null)
            {
                ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Không thể cập nhật Event.");
                model.ErrorMessage = result.ErrorMessage;
                await LoadEditOptionsAsync(model, cancellationToken);
                return View(model);
            }

            TempData["SuccessMessage"] = $"Đã cập nhật Event '{result.Event.Title}'.";
            return RedirectToAction(nameof(Detail), new { id = result.Event.Id });
        }
        catch (Exception exception) when (
            exception is HttpRequestException or
            TaskCanceledException or
            JsonException)
        {
            _logger.LogWarning(exception, "Unable to update event {EventId} from MVC.", id);
            model.ErrorMessage = "Không thể kết nối tới API. Vui lòng thử lại sau.";
            await LoadEditOptionsAsync(model, cancellationToken);
            return View(model);
        }
    }

    [Authorize(Roles = "Organizer")]
    [HttpPost("{id:int}/Submit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(
        int id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _eventApiClient.SubmitEventAsync(id, cancellationToken);
            if (result.IsUnauthorized && User.Identity?.IsAuthenticated == true)
            {
                return await EndInvalidSessionAsync(
                    result.ErrorMessage ?? "Phiên đăng nhập không còn hợp lệ.");
            }

            if (result.IsForbidden)
            {
                return RedirectToAction(nameof(AccountController.AccessDenied), "Account");
            }

            if (result.IsSuccess && result.Event is not null)
            {
                TempData["SuccessMessage"] =
                    $"Event '{result.Event.Title}' đã chuyển sang PendingApproval.";
            }
            else
            {
                TempData["EventErrorMessage"] = result.ErrorMessage ??
                    "Không thể gửi Event để duyệt.";
            }
        }
        catch (Exception exception) when (
            exception is HttpRequestException or
            TaskCanceledException or
            JsonException)
        {
            _logger.LogWarning(exception, "Unable to submit event {EventId} from MVC.", id);
            TempData["EventErrorMessage"] = "Không thể kết nối tới API. Vui lòng thử lại sau.";
        }

        return RedirectToAction(nameof(Detail), new { id });
    }

    [Authorize(Roles = "Admin,Staff")]
    [HttpPost("{id:int}/Approve")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(
        int id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _eventApiClient.ApproveEventAsync(id, cancellationToken);
            if (result.IsUnauthorized && User.Identity?.IsAuthenticated == true)
            {
                return await EndInvalidSessionAsync(
                    result.ErrorMessage ?? "Phiên đăng nhập không còn hợp lệ.");
            }

            if (result.IsForbidden)
            {
                return RedirectToAction(nameof(AccountController.AccessDenied), "Account");
            }

            if (result.IsSuccess && result.Event is not null)
            {
                TempData["SuccessMessage"] = $"Event '{result.Event.Title}' đã được Approved.";
            }
            else if (result.IsConflict)
            {
                var conflicts = result.Conflicts.Count == 0
                    ? "API không cung cấp chi tiết conflict."
                    : string.Join(
                        " | ",
                        result.Conflicts.Select(conflict =>
                            $"#{conflict.EventId} {conflict.Title} · {conflict.VenueName} · {conflict.Status} · {conflict.StartTime:dd/MM/yyyy HH:mm}-{conflict.EndTime:HH:mm}"));
                TempData["EventErrorMessage"] = $"{result.ErrorMessage} {conflicts}";
            }
            else
            {
                TempData["EventErrorMessage"] = result.ErrorMessage ?? "Không thể duyệt Event.";
            }
        }
        catch (Exception exception) when (
            exception is HttpRequestException or
            TaskCanceledException or
            JsonException)
        {
            _logger.LogWarning(exception, "Unable to approve event {EventId} from MVC.", id);
            TempData["EventErrorMessage"] = "Không thể kết nối tới API. Vui lòng thử lại sau.";
        }

        return RedirectToAction(nameof(Detail), new { id });
    }

    [Authorize(Roles = "Admin,Staff")]
    [HttpPost("{id:int}/Reject")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(
        int id,
        RejectEventViewModel model,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(model.Reason))
        {
            TempData["EventErrorMessage"] = "Lý do từ chối không được để rỗng.";
            return RedirectToAction(nameof(Detail), new { id });
        }

        try
        {
            var result = await _eventApiClient.RejectEventAsync(
                id,
                new RejectEventApiRequest { Reason = model.Reason.Trim() },
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

            if (result.IsSuccess && result.Event is not null)
            {
                TempData["SuccessMessage"] = $"Event '{result.Event.Title}' đã bị từ chối.";
            }
            else
            {
                TempData["EventErrorMessage"] = result.ErrorMessage ?? "Không thể từ chối Event.";
            }
        }
        catch (Exception exception) when (
            exception is HttpRequestException or
            TaskCanceledException or
            JsonException)
        {
            _logger.LogWarning(exception, "Unable to reject event {EventId} from MVC.", id);
            TempData["EventErrorMessage"] = "Không thể kết nối tới API. Vui lòng thử lại sau.";
        }

        return RedirectToAction(nameof(Detail), new { id });
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

    private async Task<CreateOptionsResult> LoadEditOptionsAsync(
        EditEventViewModel model,
        CancellationToken cancellationToken)
    {
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

        model.Venues = venuesResult.IsSuccess ? venuesResult.Venues : [];
        return venuesResult.IsSuccess
            ? new CreateOptionsResult(true, false, null)
            : new CreateOptionsResult(false, false, venuesResult.ErrorMessage);
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

    private static EditEventViewModel MapEdit(EventDetailApiResponse eventItem)
    {
        return new EditEventViewModel
        {
            Id = eventItem.Id,
            Title = eventItem.Title,
            Description = eventItem.Description,
            ClubId = eventItem.ClubId,
            ClubName = eventItem.ClubName,
            VenueId = eventItem.VenueId,
            StartTime = eventItem.StartTime.ToLocalTime(),
            EndTime = eventItem.EndTime.ToLocalTime(),
            RegistrationDeadline = eventItem.RegistrationDeadline.ToLocalTime(),
            Capacity = eventItem.Capacity
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
