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

    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin,Staff")]
    [HttpGet("Create")]
    public IActionResult Create()
    {
        return View(new CreateVenueViewModel());
    }

    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin,Staff")]
    [HttpPost("Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        CreateVenueViewModel model,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var result = await _venueApiClient.CreateVenueAsync(
                new CreateVenueApiRequest(
                    model.Name.Trim(),
                    model.Location.Trim(),
                    model.Capacity),
                cancellationToken);

            if (result.IsUnauthorized && User.Identity?.IsAuthenticated == true)
            {
                return await EndInvalidSessionAsync(
                    result.ErrorMessage ?? "Phiên đăng nhập không còn hợp lệ.");
            }

            if (result.IsForbidden)
            {
                Response.StatusCode = StatusCodes.Status403Forbidden;
                model.ErrorMessage = result.ErrorMessage ?? "Bạn không có quyền tạo địa điểm.";
                return View(model);
            }

            if (result.IsConflict)
            {
                ModelState.AddModelError(
                    nameof(CreateVenueViewModel.Name),
                    result.ErrorMessage ?? "Tên và vị trí địa điểm đã tồn tại.");
                model.ErrorMessage = result.ErrorMessage;
                return View(model);
            }

            if (!result.IsSuccess || result.Venue == null)
            {
                model.ErrorMessage = result.ErrorMessage ?? "Không thể tạo địa điểm. Vui lòng thử lại.";
                return View(model);
            }

            TempData["SuccessMessage"] = $"Đã tạo địa điểm '{result.Venue.Name}' thành công.";
            return RedirectToAction(
                nameof(Index),
                new { search = result.Venue.Name, page = 1, pageSize = 10 });
        }
        catch (Exception exception) when (
            exception is HttpRequestException or
            TaskCanceledException or
            JsonException)
        {
            _logger.LogWarning(exception, "Unable to create venue from MVC.");
            model.ErrorMessage = "Không thể kết nối tới API. Vui lòng thử lại sau.";
            return View(model);
        }
    }

    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin,Staff")]
    [HttpGet("{id:int}/Edit")]
    public async Task<IActionResult> Edit(
        int id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _venueApiClient.GetVenueAsync(id, cancellationToken);

            if (result.IsUnauthorized && User.Identity?.IsAuthenticated == true)
            {
                return await EndInvalidSessionAsync(
                    result.ErrorMessage ?? "Phiên đăng nhập không còn hợp lệ.");
            }

            if (!result.IsSuccess || result.Venue is null)
            {
                if (result.IsNotFound)
                {
                    Response.StatusCode = StatusCodes.Status404NotFound;
                }

                return View(new EditVenueViewModel
                {
                    Id = id,
                    IsNotFound = result.IsNotFound,
                    LoadErrorMessage = result.ErrorMessage ??
                        "Không thể tải địa điểm để chỉnh sửa."
                });
            }

            return View(MapEditVenue(result.Venue));
        }
        catch (Exception exception) when (
            exception is HttpRequestException or
            TaskCanceledException or
            JsonException)
        {
            _logger.LogWarning(exception, "Unable to load venue {VenueId} for editing.", id);
            return View(new EditVenueViewModel
            {
                Id = id,
                LoadErrorMessage = "Không thể kết nối tới API. Vui lòng thử lại sau."
            });
        }
    }

    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin,Staff")]
    [HttpPost("{id:int}/Edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int id,
        EditVenueViewModel model,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var result = await _venueApiClient.UpdateVenueAsync(
                id,
                new UpdateVenueApiRequest(
                    model.Name.Trim(),
                    model.Location.Trim(),
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
                return View(new EditVenueViewModel
                {
                    Id = id,
                    Name = model.Name,
                    Location = model.Location,
                    Capacity = model.Capacity,
                    IsNotFound = true,
                    LoadErrorMessage = result.ErrorMessage ?? "Địa điểm không tồn tại."
                });
            }

            if (result.IsConflict)
            {
                ModelState.AddModelError(
                    nameof(EditVenueViewModel.Capacity),
                    result.ErrorMessage ?? "Sức chứa mới xung đột với đăng ký hiện tại.");
                model.ErrorMessage = result.ErrorMessage;
                return View(model);
            }

            if (result.IsSuccess && result.Venue is not null)
            {
                TempData["SuccessMessage"] =
                    $"Đã cập nhật địa điểm '{result.Venue.Name}' thành công.";
                return RedirectToAction(
                    nameof(Index),
                    new { search = result.Venue.Name, page = 1, pageSize = 10 });
            }

            model.ErrorMessage = result.ErrorMessage ??
                "Không thể cập nhật địa điểm. Vui lòng thử lại.";
            return View(model);
        }
        catch (Exception exception) when (
            exception is HttpRequestException or
            TaskCanceledException or
            JsonException)
        {
            _logger.LogWarning(exception, "Unable to update venue {VenueId} from MVC.", id);
            model.ErrorMessage = "Không thể kết nối tới API. Vui lòng thử lại sau.";
            return View(model);
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

    private static EditVenueViewModel MapEditVenue(VenueApiResponse venue)
    {
        return new EditVenueViewModel
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
