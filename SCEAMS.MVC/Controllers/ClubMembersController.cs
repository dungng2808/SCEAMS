using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SCEAMS.MVC.Models.Api;
using SCEAMS.MVC.Services.ApiClients;
using SCEAMS.MVC.ViewModels;

namespace SCEAMS.MVC.Controllers;

[Route("Clubs/{clubId:int}/Members")]
[Authorize(Roles = "Organizer,Admin,Staff")]
public sealed class ClubMembersController : Controller
{
    private readonly IClubMembershipApiClient _membershipApiClient;
    private readonly IClubApiClient _clubApiClient;
    private readonly ILogger<ClubMembersController> _logger;

    public ClubMembersController(
        IClubMembershipApiClient membershipApiClient,
        IClubApiClient clubApiClient,
        ILogger<ClubMembersController> logger)
    {
        _membershipApiClient = membershipApiClient;
        _clubApiClient = clubApiClient;
        _logger = logger;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(
        int clubId,
        string? tab = "pending",
        string? search = null,
        int page = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var clubResult = await _clubApiClient.GetClubByIdAsync(clubId, cancellationToken);
            if (clubResult.IsUnauthorized && User.Identity?.IsAuthenticated == true)
            {
                return await EndInvalidSessionAsync(
                    clubResult.ErrorMessage ?? "Phiên đăng nhập không còn hợp lệ.");
            }

            if (clubResult.IsNotFound)
            {
                Response.StatusCode = StatusCodes.Status404NotFound;
                return View(new ClubMembersViewModel
                {
                    ClubId = clubId,
                    IsNotFound = true,
                    ErrorMessage = clubResult.ErrorMessage ?? "Không tìm thấy câu lạc bộ."
                });
            }

            if (clubResult.IsForbidden)
            {
                Response.StatusCode = StatusCodes.Status403Forbidden;
                return View(new ClubMembersViewModel
                {
                    ClubId = clubId,
                    IsForbidden = true,
                    ErrorMessage = clubResult.ErrorMessage ?? "Bạn không có quyền quản lý thành viên câu lạc bộ này."
                });
            }

            var clubName = clubResult.Club?.Name ?? $"CLB #{clubId}";
            var normalizedPage = Math.Max(page, 1);
            var normalizedPageSize = Math.Clamp(pageSize, 1, 50);
            var normalizedTab = string.Equals(tab, "active", StringComparison.OrdinalIgnoreCase)
                ? "active"
                : "pending";

            var membershipResult = normalizedTab == "active"
                ? await _membershipApiClient.GetActiveMembershipsAsync(
                    clubId,
                    search,
                    normalizedPage,
                    normalizedPageSize,
                    cancellationToken)
                : await _membershipApiClient.GetPendingMembershipsAsync(
                    clubId,
                    search,
                    normalizedPage,
                    normalizedPageSize,
                    cancellationToken);

            if (membershipResult.IsUnauthorized && User.Identity?.IsAuthenticated == true)
            {
                return await EndInvalidSessionAsync(
                    membershipResult.ErrorMessage ?? "Phiên đăng nhập không còn hợp lệ.");
            }

            if (membershipResult.IsForbidden)
            {
                Response.StatusCode = StatusCodes.Status403Forbidden;
                return View(new ClubMembersViewModel
                {
                    ClubId = clubId,
                    ClubName = clubName,
                    IsForbidden = true,
                    ErrorMessage = membershipResult.ErrorMessage ?? "Bạn không có quyền quản lý thành viên câu lạc bộ này."
                });
            }

            if (!membershipResult.IsSuccess)
            {
                return View(new ClubMembersViewModel
                {
                    ClubId = clubId,
                    ClubName = clubName,
                    ActiveTab = normalizedTab,
                    Search = search,
                    Page = normalizedPage,
                    PageSize = normalizedPageSize,
                    ErrorMessage = membershipResult.ErrorMessage ?? "Không thể tải danh sách thành viên."
                });
            }

            var memberItems = membershipResult.Items.Select(m => new ClubMembershipItemViewModel
            {
                Id = m.Id,
                StudentId = m.StudentId,
                StudentName = m.StudentName,
                StudentEmail = m.StudentEmail,
                RoleInClub = m.RoleInClub,
                JoinDateFormatted = m.JoinDate.ToString("dd/MM/yyyy HH:mm"),
                Status = m.Status,
                StatusLabel = normalizedTab == "active" ? "Đang hoạt động" : "Chờ duyệt",
                StatusBadgeClass = normalizedTab == "active"
                    ? "status-badge--success"
                    : "status-badge--warning",
                Initials = GetInitials(m.StudentName)
            }).ToList();

            return View(new ClubMembersViewModel
            {
                ClubId = clubId,
                ClubName = clubName,
                ActiveTab = normalizedTab,
                Search = search,
                Page = membershipResult.Page,
                PageSize = membershipResult.PageSize,
                TotalItems = membershipResult.TotalItems,
                Members = memberItems
            });
        }
        catch (Exception exception) when (
            exception is HttpRequestException or
            TaskCanceledException or
            JsonException)
        {
            _logger.LogWarning(exception, "Unable to load club membership list for club #{ClubId}.", clubId);

            return View(new ClubMembersViewModel
            {
                ClubId = clubId,
                ErrorMessage = "Không thể kết nối tới API. Vui lòng thử lại sau."
            });
        }
    }

    [HttpPost("{userId:int}/Approve")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(
        int clubId,
        int userId,
        string? search = null,
        int page = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        return await DecideMembershipAsync(
            clubId,
            userId,
            approve: true,
            rejectionReason: null,
            search,
            page,
            pageSize,
            cancellationToken);
    }

    [HttpPost("{userId:int}/Reject")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(
        int clubId,
        int userId,
        string? rejectionReason = null,
        string? search = null,
        int page = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        return await DecideMembershipAsync(
            clubId,
            userId,
            approve: false,
            rejectionReason,
            search,
            page,
            pageSize,
            cancellationToken);
    }

    [HttpPost("{userId:int}/Remove")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Remove(
        int clubId,
        int userId,
        string? reason,
        string? search = null,
        int page = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var routeValues = new
        {
            clubId,
            tab = "active",
            search,
            page = Math.Max(page, 1),
            pageSize = Math.Clamp(pageSize, 1, 50)
        };

        if (string.IsNullOrWhiteSpace(reason))
        {
            TempData["ErrorMessage"] = "Lý do loại thành viên không được để trống.";
            return RedirectToAction(nameof(Index), routeValues);
        }

        try
        {
            var result = await _membershipApiClient.RemoveMembershipAsync(
                clubId,
                userId,
                new RemoveClubMembershipApiRequest(reason.Trim()),
                cancellationToken);

            if (result.IsUnauthorized && User.Identity?.IsAuthenticated == true)
            {
                return await EndInvalidSessionAsync(
                    result.ErrorMessage ?? "Phiên đăng nhập không còn hợp lệ.");
            }

            if (result.IsForbidden || result.IsNotFound || result.IsConflict || result.IsValidationError || !result.IsSuccess)
            {
                TempData["ErrorMessage"] = result.ErrorMessage
                    ?? "Không thể loại thành viên vào lúc này.";
            }
            else
            {
                TempData["SuccessMessage"] = "Đã loại thành viên và lưu lịch sử hoạt động của câu lạc bộ.";
            }

            return RedirectToAction(nameof(Index), routeValues);
        }
        catch (Exception exception) when (
            exception is HttpRequestException or
            TaskCanceledException or
            JsonException)
        {
            _logger.LogWarning(
                exception,
                "Unable to remove member user #{UserId} from club #{ClubId}.",
                userId,
                clubId);

            TempData["ErrorMessage"] = "Không thể kết nối tới API. Vui lòng thử lại sau.";
            return RedirectToAction(nameof(Index), routeValues);
        }
    }

    private async Task<IActionResult> DecideMembershipAsync(
        int clubId,
        int userId,
        bool approve,
        string? rejectionReason,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _membershipApiClient.DecideMembershipAsync(
                clubId,
                userId,
                new DecideClubMembershipApiRequest(approve, rejectionReason),
                cancellationToken);

            if (result.IsUnauthorized && User.Identity?.IsAuthenticated == true)
            {
                return await EndInvalidSessionAsync(
                    result.ErrorMessage ?? "Phiên đăng nhập không còn hợp lệ.");
            }

            if (result.IsForbidden)
            {
                TempData["ErrorMessage"] = result.ErrorMessage
                    ?? "Bạn không có quyền xử lý đơn gia nhập của câu lạc bộ này.";
            }
            else if (result.IsNotFound)
            {
                TempData["ErrorMessage"] = result.ErrorMessage
                    ?? "Không tìm thấy đơn gia nhập cần xử lý.";
            }
            else if (result.IsConflict)
            {
                TempData["ErrorMessage"] = result.ErrorMessage
                    ?? "Đơn gia nhập đã được xử lý bởi người khác hoặc không còn chờ duyệt.";
            }
            else if (!result.IsSuccess)
            {
                TempData["ErrorMessage"] = result.ErrorMessage
                    ?? "Không thể xử lý đơn gia nhập vào lúc này.";
            }
            else
            {
                var actionLabel = approve ? "duyệt" : "từ chối";
                TempData["SuccessMessage"] = $"Đã {actionLabel} đơn gia nhập thành công. Danh sách đã được cập nhật.";
            }

            return RedirectToAction(
                nameof(Index),
                new
                {
                    clubId,
                    tab = "pending",
                    search,
                    page = Math.Max(page, 1),
                    pageSize = Math.Clamp(pageSize, 1, 50)
                });
        }
        catch (Exception exception) when (
            exception is HttpRequestException or
            TaskCanceledException or
            JsonException)
        {
            _logger.LogWarning(
                exception,
                "Unable to decide membership for user #{UserId} in club #{ClubId}.",
                userId,
                clubId);

            TempData["ErrorMessage"] = "Không thể kết nối tới API. Vui lòng thử lại sau.";
            return RedirectToAction(
                nameof(Index),
                new
                {
                    clubId,
                    tab = "pending",
                    search,
                    page = Math.Max(page, 1),
                    pageSize = Math.Clamp(pageSize, 1, 50)
                });
        }
    }

    private static string GetInitials(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "SV";
        var words = name.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return words.Length switch
        {
            0 => "SV",
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
