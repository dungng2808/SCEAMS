using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
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
            var isStudent = User.IsInRole("Student");
            var isOwner = currentUserId.HasValue && currentUserId.Value == club.CreatedByUserId;

            var (label, badgeClass) = GetStatusInfo(club.Status);
            var isApproved = string.Equals(club.Status, "Approved", StringComparison.OrdinalIgnoreCase);
            var isPending = string.Equals(club.Status, "PendingApproval", StringComparison.OrdinalIgnoreCase);
            var isDissolved = string.Equals(club.Status, "Dissolved", StringComparison.OrdinalIgnoreCase);

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
                CanEdit = !isDissolved && (isOwner || isAdmin),
                CanApproveOrReject = !isDissolved && (isAdmin || isStaff) && isPending,
                CanDissolve = !isDissolved && (isAdmin || isStaff) && isApproved,
                CanJoin = !isDissolved && isApproved && isStudent
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

    [HttpGet("Pending")]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> Pending(CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _clubApiClient.GetClubsAsync(
                new ClubListApiQuery(Status: "PendingApproval", PageSize: 50),
                cancellationToken);

            if (result.IsUnauthorized && User.Identity?.IsAuthenticated == true)
            {
                return await EndInvalidSessionAsync(
                    result.ErrorMessage ?? "Phiên đăng nhập không còn hợp lệ.");
            }

            if (result.IsForbidden)
            {
                Response.StatusCode = StatusCodes.Status403Forbidden;
                return View(new PendingClubsViewModel
                {
                    IsForbidden = true,
                    ErrorMessage = result.ErrorMessage ?? "Bạn không có quyền xem danh sách chờ duyệt."
                });
            }

            if (!result.IsSuccess)
            {
                return View(new PendingClubsViewModel
                {
                    ErrorMessage = result.ErrorMessage ?? "Không thể tải danh sách chờ duyệt."
                });
            }

            var pendingClubs = result.Clubs
                .Select(MapClub)
                .ToList();

            return View(new PendingClubsViewModel
            {
                PendingClubs = pendingClubs
            });
        }
        catch (Exception exception) when (
            exception is HttpRequestException or
            TaskCanceledException or
            JsonException)
        {
            _logger.LogWarning(exception, "Unable to load pending clubs queue.");

            return View(new PendingClubsViewModel
            {
                ErrorMessage = "Không thể kết nối tới API. Vui lòng thử lại sau."
            });
        }
    }

    [HttpPost("{id:int}/Approve")]
    [Authorize(Roles = "Admin,Staff")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(
        int id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _clubApiClient.ApproveClubAsync(id, cancellationToken);

            if (result.IsUnauthorized && User.Identity?.IsAuthenticated == true)
            {
                return await EndInvalidSessionAsync(
                    result.ErrorMessage ?? "Phiên đăng nhập không còn hợp lệ.");
            }

            if (result.IsForbidden)
            {
                TempData["ErrorMessage"] = result.ErrorMessage ?? "Bạn không có quyền duyệt câu lạc bộ này.";
                return RedirectToAction(nameof(Details), new { id });
            }

            if (!result.IsSuccess || result.Club == null)
            {
                TempData["ErrorMessage"] = result.ErrorMessage ?? "Không thể duyệt câu lạc bộ vào lúc này.";
                return RedirectToAction(nameof(Details), new { id });
            }

            TempData["SuccessMessage"] = $"Đã duyệt thành công câu lạc bộ '{result.Club.Name}'! Câu lạc bộ hiện đã chính thức hoạt động và công khai.";
            return RedirectToAction(nameof(Details), new { id = result.Club.Id });
        }
        catch (Exception exception) when (
            exception is HttpRequestException or
            TaskCanceledException or
            JsonException)
        {
            _logger.LogWarning(exception, "Unable to send approve club #{ClubId} request.", id);

            TempData["ErrorMessage"] = "Không thể kết nối tới API. Vui lòng thử lại sau.";
            return RedirectToAction(nameof(Details), new { id });
        }
    }

    [HttpPost("{id:int}/Reject")]
    [Authorize(Roles = "Admin,Staff")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(
        int id,
        RejectClubViewModel model,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(model.Reason))
        {
            TempData["ErrorMessage"] = "Lý do từ chối không được để rỗng.";
            return RedirectToAction(nameof(Details), new { id });
        }

        try
        {
            var apiRequest = new RejectClubApiRequest
            {
                Reason = model.Reason.Trim()
            };

            var result = await _clubApiClient.RejectClubAsync(id, apiRequest, cancellationToken);

            if (result.IsUnauthorized && User.Identity?.IsAuthenticated == true)
            {
                return await EndInvalidSessionAsync(
                    result.ErrorMessage ?? "Phiên đăng nhập không còn hợp lệ.");
            }

            if (result.IsForbidden)
            {
                TempData["ErrorMessage"] = result.ErrorMessage ?? "Bạn không có quyền từ chối câu lạc bộ này.";
                return RedirectToAction(nameof(Details), new { id });
            }

            if (!result.IsSuccess || result.Club == null)
            {
                TempData["ErrorMessage"] = result.ErrorMessage ?? "Không thể từ chối câu lạc bộ vào lúc này.";
                return RedirectToAction(nameof(Details), new { id });
            }

            TempData["SuccessMessage"] = $"Đã từ chối đề xuất câu lạc bộ '{result.Club.Name}'.";
            return RedirectToAction(nameof(Details), new { id = result.Club.Id });
        }
        catch (Exception exception) when (
            exception is HttpRequestException or
            TaskCanceledException or
            JsonException)
        {
            _logger.LogWarning(exception, "Unable to send reject club #{ClubId} request.", id);

            TempData["ErrorMessage"] = "Không thể kết nối tới API. Vui lòng thử lại sau.";
            return RedirectToAction(nameof(Details), new { id });
        }
    }

    [HttpPost("{id:int}/Dissolve")]
    [Authorize(Roles = "Admin,Staff")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Dissolve(
        int id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _clubApiClient.DissolveClubAsync(id, cancellationToken);

            if (result.IsUnauthorized && User.Identity?.IsAuthenticated == true)
            {
                return await EndInvalidSessionAsync(
                    result.ErrorMessage ?? "Phiên đăng nhập không còn hợp lệ.");
            }

            if (result.IsForbidden)
            {
                TempData["ErrorMessage"] = result.ErrorMessage ?? "Bạn không có quyền giải thể câu lạc bộ này.";
                return RedirectToAction(nameof(Details), new { id });
            }

            if (!result.IsSuccess || result.Club == null)
            {
                TempData["ErrorMessage"] = result.ErrorMessage ?? "Không thể giải thể câu lạc bộ vào lúc này.";
                return RedirectToAction(nameof(Details), new { id });
            }

            TempData["SuccessMessage"] = $"Câu lạc bộ '{result.Club.Name}' đã chính thức giải thể.";
            return RedirectToAction(nameof(Details), new { id = result.Club.Id });
        }
        catch (Exception exception) when (
            exception is HttpRequestException or
            TaskCanceledException or
            JsonException)
        {
            _logger.LogWarning(exception, "Unable to send dissolve club #{ClubId} request.", id);

            TempData["ErrorMessage"] = "Không thể kết nối tới API. Vui lòng thử lại sau.";
            return RedirectToAction(nameof(Details), new { id });
        }
    }

    [HttpPost("{id:int}/Join")]
    [Authorize(Roles = "Student")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Join(
        int id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _clubApiClient.RequestJoinClubAsync(id, cancellationToken);

            if (result.IsUnauthorized && User.Identity?.IsAuthenticated == true)
            {
                return await EndInvalidSessionAsync(
                    result.ErrorMessage ?? "Phiên đăng nhập không còn hợp lệ.");
            }

            if (result.IsForbidden)
            {
                TempData["ErrorMessage"] = result.ErrorMessage ?? "Chỉ tài khoản Sinh viên (Student) mới có thể gia nhập câu lạc bộ.";
                return RedirectToAction(nameof(Details), new { id });
            }

            if (result.IsConflict)
            {
                TempData["ErrorMessage"] = result.ErrorMessage ?? "Bạn không thể gửi đơn xin gia nhập vào lúc này.";
                return RedirectToAction(nameof(Details), new { id });
            }

            if (!result.IsSuccess || result.Membership == null)
            {
                TempData["ErrorMessage"] = result.ErrorMessage ?? "Không thể gửi đơn xin gia nhập vào lúc này.";
                return RedirectToAction(nameof(Details), new { id });
            }

            TempData["SuccessMessage"] = $"Đơn xin gia nhập câu lạc bộ '{result.Membership.ClubName}' đã được gửi thành công! Trạng thái hiện tại: Đang chờ duyệt.";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (Exception exception) when (
            exception is HttpRequestException or
            TaskCanceledException or
            JsonException)
        {
            _logger.LogWarning(exception, "Unable to send request join club #{ClubId}.", id);

            TempData["ErrorMessage"] = "Không thể kết nối tới API. Vui lòng thử lại sau.";
            return RedirectToAction(nameof(Details), new { id });
        }
    }

    [HttpGet("Create")]
    [Authorize(Roles = "Organizer,Admin")]
    public async Task<IActionResult> Create(CancellationToken cancellationToken = default)
    {
        var categoryOptions = await GetCategoryOptionsAsync(cancellationToken);
        return View(new CreateClubViewModel { Categories = categoryOptions });
    }

    [HttpPost("Create")]
    [Authorize(Roles = "Organizer,Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        CreateClubViewModel model,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            model.Categories = await GetCategoryOptionsAsync(cancellationToken);
            return View(model);
        }

        try
        {
            var apiRequest = new CreateClubApiRequest
            {
                Name = model.Name.Trim(),
                Description = model.Description?.Trim(),
                CategoryId = model.CategoryId
            };

            var result = await _clubApiClient.CreateClubAsync(apiRequest, cancellationToken);

            if (result.IsUnauthorized && User.Identity?.IsAuthenticated == true)
            {
                return await EndInvalidSessionAsync(
                    result.ErrorMessage ?? "Phiên đăng nhập không còn hợp lệ.");
            }

            if (result.IsForbidden)
            {
                Response.StatusCode = StatusCodes.Status403Forbidden;
                model.Categories = await GetCategoryOptionsAsync(cancellationToken);
                model.ErrorMessage = result.ErrorMessage ?? "Bạn không có quyền gửi đề xuất câu lạc bộ.";
                return View(model);
            }

            if (!result.IsSuccess || result.Club == null)
            {
                model.Categories = await GetCategoryOptionsAsync(cancellationToken);
                model.ErrorMessage = result.ErrorMessage ?? "Không thể gửi đề xuất câu lạc bộ. Vui lòng kiểm tra lại.";
                return View(model);
            }

            TempData["SuccessMessage"] = $"Đề xuất thành lập câu lạc bộ '{result.Club.Name}' đã được gửi thành công! Trạng thái hiện tại: Chờ duyệt.";
            return RedirectToAction(nameof(Details), new { id = result.Club.Id });
        }
        catch (Exception exception) when (
            exception is HttpRequestException or
            TaskCanceledException or
            JsonException)
        {
            _logger.LogWarning(exception, "Unable to send create club request to API.");

            model.Categories = await GetCategoryOptionsAsync(cancellationToken);
            model.ErrorMessage = "Không thể kết nối tới API. Vui lòng thử lại sau.";
            return View(model);
        }
    }

    [HttpGet("{id:int}/Edit")]
    [Authorize(Roles = "Organizer,Admin")]
    public async Task<IActionResult> Edit(
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
                return View("Edit", new EditClubViewModel
                {
                    Id = id,
                    IsNotFound = true,
                    ErrorMessage = result.ErrorMessage ?? "Không tìm thấy câu lạc bộ."
                });
            }

            if (result.IsForbidden || result.Club == null)
            {
                Response.StatusCode = StatusCodes.Status403Forbidden;
                return View("Edit", new EditClubViewModel
                {
                    Id = id,
                    IsForbidden = true,
                    ErrorMessage = result.ErrorMessage ?? "Bạn không có quyền chỉnh sửa câu lạc bộ này."
                });
            }

            var club = result.Club;
            var currentUserId = GetCurrentUserId();
            var isAdmin = User.IsInRole("Admin");
            var isOwner = currentUserId.HasValue && currentUserId.Value == club.CreatedByUserId;

            if (!isAdmin && !isOwner)
            {
                Response.StatusCode = StatusCodes.Status403Forbidden;
                return View("Edit", new EditClubViewModel
                {
                    Id = id,
                    IsForbidden = true,
                    ErrorMessage = "Bạn không phải là chủ sở hữu câu lạc bộ này."
                });
            }

            var (label, badgeClass) = GetStatusInfo(club.Status);
            var categoryOptions = await GetCategoryOptionsAsync(cancellationToken);

            var viewModel = new EditClubViewModel
            {
                Id = club.Id,
                Name = club.Name,
                Description = club.Description,
                CategoryId = club.CategoryId,
                StatusLabel = label,
                StatusBadgeClass = badgeClass,
                Categories = categoryOptions
            };

            return View(viewModel);
        }
        catch (Exception exception) when (
            exception is HttpRequestException or
            TaskCanceledException or
            JsonException)
        {
            _logger.LogWarning(exception, "Unable to load club details #{ClubId} for edit.", id);

            return View("Edit", new EditClubViewModel
            {
                Id = id,
                ErrorMessage = "Không thể kết nối tới API. Vui lòng thử lại sau."
            });
        }
    }

    [HttpPost("{id:int}/Edit")]
    [Authorize(Roles = "Organizer,Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int id,
        EditClubViewModel model,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            model.Categories = await GetCategoryOptionsAsync(cancellationToken);
            return View(model);
        }

        try
        {
            var apiRequest = new UpdateClubApiRequest
            {
                Name = model.Name.Trim(),
                Description = model.Description?.Trim(),
                CategoryId = model.CategoryId
            };

            var result = await _clubApiClient.UpdateClubAsync(id, apiRequest, cancellationToken);

            if (result.IsUnauthorized && User.Identity?.IsAuthenticated == true)
            {
                return await EndInvalidSessionAsync(
                    result.ErrorMessage ?? "Phiên đăng nhập không còn hợp lệ.");
            }

            if (result.IsForbidden)
            {
                Response.StatusCode = StatusCodes.Status403Forbidden;
                model.IsForbidden = true;
                model.ErrorMessage = result.ErrorMessage ?? "Bạn không còn quyền chỉnh sửa câu lạc bộ này (Quyền sở hữu có thể đã thay đổi).";
                model.Categories = await GetCategoryOptionsAsync(cancellationToken);
                return View(model);
            }

            if (!result.IsSuccess || result.Club == null)
            {
                model.ErrorMessage = result.ErrorMessage ?? "Không thể cập nhật thông tin câu lạc bộ vào lúc này.";
                model.Categories = await GetCategoryOptionsAsync(cancellationToken);
                return View(model);
            }

            TempData["SuccessMessage"] = $"Đã cập nhật thông tin câu lạc bộ '{result.Club.Name}' thành công!";
            return RedirectToAction(nameof(Details), new { id = result.Club.Id });
        }
        catch (Exception exception) when (
            exception is HttpRequestException or
            TaskCanceledException or
            JsonException)
        {
            _logger.LogWarning(exception, "Unable to send update club #{ClubId} request.", id);

            model.ErrorMessage = "Không thể kết nối tới API. Vui lòng thử lại sau.";
            model.Categories = await GetCategoryOptionsAsync(cancellationToken);
            return View(model);
        }
    }

    private async Task<List<ClubCategorySelectItemViewModel>> GetCategoryOptionsAsync(CancellationToken cancellationToken)
    {
        var categoryResult = await _clubCategoryApiClient.GetClubCategoriesAsync(cancellationToken);
        if (categoryResult.IsSuccess)
        {
            return categoryResult.Categories
                .Select(c => new ClubCategorySelectItemViewModel { Id = c.Id, Name = c.Name })
                .ToList();
        }

        return [];
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
