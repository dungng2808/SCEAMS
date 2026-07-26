using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using SCEAMS.MVC.Models.Api;
using SCEAMS.MVC.Services.ApiClients;
using SCEAMS.MVC.ViewModels;

namespace SCEAMS.MVC.Controllers;

[Route("Clubs")]
public sealed class ClubsController : Controller
{
    private static readonly string[] Themes =
        ["blue", "violet", "amber", "emerald"];

    private readonly IClubApiClient _clubApiClient;
    private readonly IClubCategoryApiClient _clubCategoryApiClient;
    private readonly ILogger<ClubsController> _logger;

    public ClubsController(
        IClubApiClient clubApiClient,
        IClubCategoryApiClient clubCategoryApiClient,
        ILogger<ClubsController> logger)
    {
        _clubApiClient = clubApiClient;
        _clubCategoryApiClient = clubCategoryApiClient;
        _logger = logger;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(
        int? categoryId,
        string? search,
        string? status,
        string? sortBy,
        int page = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var canManage = User.IsInRole("Admin") || User.IsInRole("Staff");
        var canCreateClub = User.IsInRole("Organizer") || User.IsInRole("Admin");

        var effectiveStatus = canManage ? status : "Approved";
        var normalizedPage = Math.Max(page, 1);
        var normalizedPageSize = Math.Clamp(pageSize, 1, 50);

        try
        {
            var categoriesTask = _clubCategoryApiClient.GetClubCategoriesAsync(cancellationToken);
            var clubsTask = _clubApiClient.GetClubsAsync(
                new ClubListApiQuery(
                    categoryId,
                    search,
                    effectiveStatus,
                    sortBy,
                    normalizedPage,
                    normalizedPageSize),
                cancellationToken);

            await Task.WhenAll(categoriesTask, clubsTask);

            var categoryResult = categoriesTask.Result;
            var clubResult = clubsTask.Result;

            if (clubResult.IsUnauthorized && User.Identity?.IsAuthenticated == true)
            {
                return await EndInvalidSessionAsync(
                    clubResult.ErrorMessage ?? "Phiên đăng nhập không còn hợp lệ.");
            }

            if (clubResult.IsForbidden)
            {
                Response.StatusCode = StatusCodes.Status403Forbidden;
                return View(new ClubsViewModel
                {
                    CanManage = canManage,
                    CanCreateClub = canCreateClub,
                    IsForbidden = true,
                    ErrorMessage = clubResult.ErrorMessage ?? "Bạn không có quyền xem danh sách này."
                });
            }

            var categoryOptions = categoryResult.IsSuccess
                ? categoryResult.Categories
                    .Select(c => new ClubCategorySelectItemViewModel { Id = c.Id, Name = c.Name })
                    .ToList()
                : [];

            if (!clubResult.IsSuccess)
            {
                return View(new ClubsViewModel
                {
                    CategoryId = categoryId,
                    Search = search,
                    Status = effectiveStatus,
                    SortBy = sortBy ?? "name_asc",
                    Page = normalizedPage,
                    PageSize = normalizedPageSize,
                    CanManage = canManage,
                    CanCreateClub = canCreateClub,
                    ErrorMessage = clubResult.ErrorMessage ?? "Không thể tải danh sách câu lạc bộ.",
                    Categories = categoryOptions
                });
            }

            return View(new ClubsViewModel
            {
                CategoryId = categoryId,
                Search = search,
                Status = effectiveStatus,
                SortBy = sortBy ?? "name_asc",
                Page = clubResult.Page,
                PageSize = clubResult.PageSize,
                TotalItems = clubResult.TotalItems,
                TotalPages = clubResult.TotalPages,
                HasPreviousPage = clubResult.HasPreviousPage,
                HasNextPage = clubResult.HasNextPage,
                CanManage = canManage,
                CanCreateClub = canCreateClub,
                Categories = categoryOptions,
                Clubs = clubResult.Clubs
                    .Select(MapClub)
                    .ToList()
            });
        }
        catch (Exception exception) when (
            exception is HttpRequestException or
            TaskCanceledException or
            JsonException)
        {
            _logger.LogWarning(exception, "Unable to load clubs from the API.");

            return View(new ClubsViewModel
            {
                CanManage = canManage,
                CanCreateClub = canCreateClub,
                ErrorMessage = "Không thể kết nối tới API. Vui lòng thử lại sau."
            });
        }
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Details(
        int id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _clubApiClient.GetClubByIdAsync(id, cancellationToken);

            if (result.IsUnauthorized && User.Identity?.IsAuthenticated == true)
            {
                return await EndInvalidSessionAsync(
                    result.ErrorMessage ?? "Phiên đăng nhập không còn hợp lệ.");
            }

            if (result.IsNotFound)
            {
                Response.StatusCode = StatusCodes.Status404NotFound;
                return View("Details", new ClubDetailsViewModel
                {
                    Id = id,
                    IsNotFound = true,
                    ErrorMessage = result.ErrorMessage ?? "Không tìm thấy câu lạc bộ."
                });
            }

            if (result.IsForbidden)
            {
                Response.StatusCode = StatusCodes.Status403Forbidden;
                return View("Details", new ClubDetailsViewModel
                {
                    Id = id,
                    IsForbidden = true,
                    ErrorMessage = result.ErrorMessage ?? "Bạn không có quyền xem thông tin câu lạc bộ này."
                });
            }

            if (!result.IsSuccess || result.Club == null)
            {
                return View("Details", new ClubDetailsViewModel
                {
                    Id = id,
                    ErrorMessage = result.ErrorMessage ?? "Không thể tải chi tiết câu lạc bộ."
                });
            }

            var club = result.Club;
            var currentUserId = GetCurrentUserId();
            var isAdmin = User.IsInRole("Admin");
            var isStaff = User.IsInRole("Staff");
            var isOwner = currentUserId.HasValue && currentUserId.Value == club.CreatedByUserId;

            var (label, badgeClass) = GetStatusInfo(club.Status);
            var isApproved = string.Equals(club.Status, "Approved", StringComparison.OrdinalIgnoreCase);
            var isPending = string.Equals(club.Status, "PendingApproval", StringComparison.OrdinalIgnoreCase);

            var viewModel = new ClubDetailsViewModel
            {
                Id = club.Id,
                Name = club.Name,
                Description = club.Description,
                CategoryId = club.CategoryId,
                CategoryName = club.CategoryName,
                Status = club.Status,
                StatusLabel = label,
                StatusBadgeClass = badgeClass,
                CreatedByUserId = club.CreatedByUserId,
                CreatedByUserName = club.CreatedByUserName,
                ActiveMemberCount = club.ActiveMemberCount,
                CreatedAtFormatted = club.CreatedAt.ToString("dd/MM/yyyy HH:mm"),
                ReviewedAtFormatted = club.ReviewedAt?.ToString("dd/MM/yyyy HH:mm"),
                RejectionReason = club.RejectionReason,
                DissolvedAtFormatted = club.DissolvedAt?.ToString("dd/MM/yyyy HH:mm"),
                Initials = GetInitials(club.Name),
                CanEdit = isOwner || isAdmin,
                CanApproveOrReject = (isAdmin || isStaff) && isPending,
                CanDissolve = (isAdmin || isStaff) && isApproved,
                CanJoin = isApproved
            };

            return View(viewModel);
        }
        catch (Exception exception) when (
            exception is HttpRequestException or
            TaskCanceledException or
            JsonException)
        {
            _logger.LogWarning(exception, "Unable to load club details #{ClubId} from API.", id);

            return View("Details", new ClubDetailsViewModel
            {
                Id = id,
                ErrorMessage = "Không thể kết nối tới API. Vui lòng thử lại sau."
            });
        }
    }

    private int? GetCurrentUserId()
    {
        var subClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;

        return int.TryParse(subClaim, out var userId) ? userId : null;
    }

    private static ClubListItemViewModel MapClub(ClubApiResponse club, int index)
    {
        var (label, badgeClass) = GetStatusInfo(club.Status);

        return new ClubListItemViewModel
        {
            Id = club.Id,
            Name = club.Name,
            Description = club.Description,
            CategoryId = club.CategoryId,
            CategoryName = club.CategoryName,
            Status = club.Status,
            StatusLabel = label,
            StatusBadgeClass = badgeClass,
            CreatedByUserId = club.CreatedByUserId,
            CreatedByUserName = club.CreatedByUserName,
            ActiveMemberCount = club.ActiveMemberCount,
            CreatedAtFormatted = club.CreatedAt.ToString("dd/MM/yyyy"),
            Initials = GetInitials(club.Name),
            Theme = Themes[index % Themes.Length]
        };
    }

    private static (string Label, string BadgeClass) GetStatusInfo(string status)
    {
        return status?.ToLowerInvariant() switch
        {
            "approved" => ("Đã duyệt", "status-badge--success"),
            "pendingapproval" => ("Chờ duyệt", "status-badge--warning"),
            "rejected" => ("Từ chối", "status-badge--danger"),
            "dissolved" => ("Đã giải thể", "status-badge--dark"),
            _ => (status ?? "Không xác định", "status-badge--neutral")
        };
    }

    private static string GetInitials(string name)
    {
        var words = name
            .Split(
                ' ',
                StringSplitOptions.RemoveEmptyEntries |
                StringSplitOptions.TrimEntries);

        return words.Length switch
        {
            0 => "CL",
            1 => words[0][..1].ToUpperInvariant(),
            _ => string.Concat(words[0][0], words[^1][0]).ToUpperInvariant()
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
