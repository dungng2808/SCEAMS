using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SCEAMS.MVC.Models.Api;
using SCEAMS.MVC.Services.ApiClients;
using SCEAMS.MVC.ViewModels;

namespace SCEAMS.MVC.Controllers;

[Authorize(Roles = "Admin")]
[Route("Admin/Users")]
public sealed class AdminUsersController : Controller
{
    private static readonly HashSet<string> CreateUserFields =
        new(
        [
            nameof(CreateAdminUserViewModel.FullName),
            nameof(CreateAdminUserViewModel.Email),
            nameof(CreateAdminUserViewModel.StudentCode),
            nameof(CreateAdminUserViewModel.PhoneNumber),
            nameof(CreateAdminUserViewModel.Role),
            nameof(CreateAdminUserViewModel.IsActive),
            nameof(CreateAdminUserViewModel.Password),
            nameof(CreateAdminUserViewModel.ConfirmPassword)
        ],
        StringComparer.OrdinalIgnoreCase);

    private static readonly HashSet<string> EditUserFields =
        new(
        [
            nameof(EditAdminUserViewModel.FullName),
            nameof(EditAdminUserViewModel.Email),
            nameof(EditAdminUserViewModel.StudentCode),
            nameof(EditAdminUserViewModel.PhoneNumber)
        ],
        StringComparer.OrdinalIgnoreCase);

    private static readonly string[] AllowedRoles =
        ["Admin", "Staff", "Organizer", "Student"];

    private static readonly int[] AllowedPageSizes =
        [5, 10, 25, 50];

    private static readonly TimeZoneInfo BusinessTimeZone =
        ResolveBusinessTimeZone();

    private readonly IUserApiClient _userApiClient;
    private readonly ILogger<AdminUsersController> _logger;

    public AdminUsersController(
        IUserApiClient userApiClient,
        ILogger<AdminUsersController> logger)
    {
        _userApiClient = userApiClient;
        _logger = logger;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(
        string? search,
        string? role,
        bool? isActive,
        int page = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var normalizedSearch = NormalizeSearch(search);
        var normalizedRole = NormalizeRole(role);
        var normalizedPage = Math.Max(page, 1);
        var normalizedPageSize = AllowedPageSizes.Contains(pageSize)
            ? pageSize
            : 10;

        try
        {
            var result = await _userApiClient.GetUsersAsync(
                new UserListApiQuery(
                    normalizedSearch,
                    normalizedRole,
                    isActive,
                    normalizedPage,
                    normalizedPageSize),
                cancellationToken);

            if (result.IsUnauthorized)
            {
                return await EndInvalidSessionAsync(
                    result.ErrorMessage ??
                    "Phiên đăng nhập không còn hợp lệ.");
            }

            if (result.IsForbidden)
            {
                Response.StatusCode =
                    StatusCodes.Status403Forbidden;

                return View(CreateViewModel(
                    normalizedSearch,
                    normalizedRole,
                    isActive,
                    normalizedPage,
                    normalizedPageSize,
                    isForbidden: true,
                    errorMessage: result.ErrorMessage));
            }

            if (!result.IsSuccess || result.Users is null)
            {
                return View(CreateViewModel(
                    normalizedSearch,
                    normalizedRole,
                    isActive,
                    normalizedPage,
                    normalizedPageSize,
                    errorMessage: result.ErrorMessage ??
                        "Không thể tải danh sách người dùng."));
            }

            var users = result.Users;

            if (users.TotalPages > 0 &&
                normalizedPage > users.TotalPages)
            {
                return RedirectToAction(
                    nameof(Index),
                    new
                    {
                        search = normalizedSearch,
                        role = normalizedRole,
                        isActive,
                        page = users.TotalPages,
                        pageSize = normalizedPageSize
                    });
            }

            return View(new AdminUsersViewModel
            {
                Search = normalizedSearch,
                Role = normalizedRole,
                IsActive = isActive,
                Page = users.Page,
                PageSize = users.PageSize,
                TotalItems = users.TotalItems,
                TotalPages = users.TotalPages,
                HasPreviousPage = users.HasPreviousPage,
                HasNextPage = users.HasNextPage,
                Users = users.Items
                    .Select(MapUser)
                    .ToList()
            });
        }
        catch (Exception exception) when (
            exception is HttpRequestException or
            TaskCanceledException or
            JsonException)
        {
            _logger.LogWarning(
                exception,
                "Unable to load the Admin user list.");

            return View(CreateViewModel(
                normalizedSearch,
                normalizedRole,
                isActive,
                normalizedPage,
                normalizedPageSize,
                errorMessage:
                    "Không thể kết nối tới API. Vui lòng thử lại sau."));
        }
    }

    [HttpGet("Create")]
    public IActionResult Create()
    {
        return View(new CreateAdminUserViewModel());
    }

    [HttpPost("Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        CreateAdminUserViewModel model,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var result = await _userApiClient.CreateUserAsync(
                new CreateUserApiRequest(
                    model.FullName,
                    model.Email,
                    model.StudentCode,
                    model.PhoneNumber,
                    model.Role,
                    model.IsActive,
                    model.Password,
                    model.ConfirmPassword),
                cancellationToken);

            if (result.IsUnauthorized)
            {
                return await EndInvalidSessionAsync(
                    result.ErrorMessage ??
                    "Phiên đăng nhập không còn hợp lệ.");
            }

            if (result.IsForbidden)
            {
                return RedirectToAction(
                    nameof(AccountController.AccessDenied),
                    "Account");
            }

            if (result.IsSuccess && result.User is not null)
            {
                TempData["UserCreatedSuccess"] =
                    $"Đã tạo tài khoản {result.User.Email} thành công.";

                return RedirectToAction(
                    nameof(Index),
                    new
                    {
                        search = result.User.Email
                    });
            }

            AddApiValidationErrors(
                result.FieldErrors,
                CreateUserFields);

            if (result.FieldErrors.Count == 0)
            {
                ModelState.AddModelError(
                    string.Empty,
                    result.ErrorMessage ??
                    "Không thể tạo tài khoản vào lúc này.");
            }
        }
        catch (Exception exception) when (
            exception is HttpRequestException or
            TaskCanceledException or
            JsonException)
        {
            _logger.LogWarning(
                exception,
                "Unable to create a user from the Admin portal.");

            ModelState.AddModelError(
                string.Empty,
                "Không thể kết nối tới API. Vui lòng thử lại sau.");
        }

        return View(model);
    }

    [HttpGet("{id:int}/Edit")]
    public async Task<IActionResult> Edit(
        int id,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _userApiClient.GetUserAsync(
                id,
                cancellationToken);

            if (result.IsUnauthorized)
            {
                return await EndInvalidSessionAsync(
                    result.ErrorMessage ??
                    "Phiên đăng nhập không còn hợp lệ.");
            }

            if (result.IsForbidden)
            {
                return RedirectToAction(
                    nameof(AccountController.AccessDenied),
                    "Account");
            }

            if (!result.IsSuccess || result.User is null)
            {
                if (result.IsNotFound)
                {
                    Response.StatusCode =
                        StatusCodes.Status404NotFound;
                }

                return View(new EditAdminUserViewModel
                {
                    Id = id,
                    IsNotFound = result.IsNotFound,
                    LoadErrorMessage = result.ErrorMessage ??
                        "Không thể tải thông tin tài khoản."
                });
            }

            return View(MapEditUser(result.User));
        }
        catch (Exception exception) when (
            exception is HttpRequestException or
            TaskCanceledException or
            JsonException)
        {
            _logger.LogWarning(
                exception,
                "Unable to load user {UserId} for editing.",
                id);

            return View(new EditAdminUserViewModel
            {
                Id = id,
                LoadErrorMessage =
                    "Không thể kết nối tới API. Vui lòng thử lại sau."
            });
        }
    }

    [HttpPost("{id:int}/Edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int id,
        EditAdminUserViewModel model,
        CancellationToken cancellationToken)
    {
        model.Id = id;

        try
        {
            var currentResult = await _userApiClient.GetUserAsync(
                id,
                cancellationToken);

            if (currentResult.IsUnauthorized)
            {
                return await EndInvalidSessionAsync(
                    currentResult.ErrorMessage ??
                    "Phiên đăng nhập không còn hợp lệ.");
            }

            if (currentResult.IsForbidden)
            {
                return RedirectToAction(
                    nameof(AccountController.AccessDenied),
                    "Account");
            }

            if (!currentResult.IsSuccess ||
                currentResult.User is null)
            {
                model.IsNotFound = currentResult.IsNotFound;
                model.LoadErrorMessage =
                    currentResult.ErrorMessage ??
                    "Không thể tải thông tin tài khoản.";

                if (currentResult.IsNotFound)
                {
                    Response.StatusCode =
                        StatusCodes.Status404NotFound;
                }

                return View(model);
            }

            DecorateEditUser(
                model,
                currentResult.User);

            if (currentResult.User.Role == "Student" &&
                string.IsNullOrWhiteSpace(model.StudentCode))
            {
                ModelState.AddModelError(
                    nameof(model.StudentCode),
                    "Vui lòng nhập mã sinh viên cho tài khoản sinh viên.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var updateResult = await _userApiClient.UpdateUserAsync(
                id,
                new UpdateUserProfileApiRequest(
                    model.FullName,
                    model.Email,
                    model.StudentCode,
                    model.PhoneNumber),
                cancellationToken);

            if (updateResult.IsUnauthorized)
            {
                return await EndInvalidSessionAsync(
                    updateResult.ErrorMessage ??
                    "Phiên đăng nhập không còn hợp lệ.");
            }

            if (updateResult.IsForbidden)
            {
                return RedirectToAction(
                    nameof(AccountController.AccessDenied),
                    "Account");
            }

            if (updateResult.IsNotFound)
            {
                Response.StatusCode =
                    StatusCodes.Status404NotFound;
                model.IsNotFound = true;
                model.LoadErrorMessage =
                    updateResult.ErrorMessage;

                return View(model);
            }

            if (!updateResult.IsSuccess ||
                updateResult.User is null)
            {
                AddApiValidationErrors(
                    updateResult.FieldErrors,
                    EditUserFields);

                if (updateResult.FieldErrors.Count == 0)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        updateResult.ErrorMessage ??
                        "Không thể cập nhật tài khoản vào lúc này.");
                }

                return View(model);
            }

            var confirmedResult = await _userApiClient.GetUserAsync(
                id,
                cancellationToken);

            if (confirmedResult.IsUnauthorized)
            {
                return await EndInvalidSessionAsync(
                    confirmedResult.ErrorMessage ??
                    "Phiên đăng nhập không còn hợp lệ.");
            }

            if (confirmedResult.IsForbidden)
            {
                return RedirectToAction(
                    nameof(AccountController.AccessDenied),
                    "Account");
            }

            if (confirmedResult.IsSuccess &&
                confirmedResult.User is not null)
            {
                TempData["UserUpdatedSuccess"] =
                    $"Đã cập nhật và xác nhận tài khoản " +
                    $"{confirmedResult.User.Email} từ API.";

                return RedirectToAction(
                    nameof(Index),
                    new
                    {
                        search = confirmedResult.User.Email
                    });
            }

            DecorateEditUser(
                model,
                updateResult.User);
            ModelState.AddModelError(
                string.Empty,
                "Thông tin đã được lưu nhưng chưa thể tải lại " +
                "tài khoản từ API để xác nhận.");
        }
        catch (Exception exception) when (
            exception is HttpRequestException or
            TaskCanceledException or
            JsonException)
        {
            _logger.LogWarning(
                exception,
                "Unable to update user {UserId} from the Admin portal.",
                id);

            const string connectionError =
                "Không thể kết nối tới API. Vui lòng thử lại sau.";

            if (string.IsNullOrWhiteSpace(model.Role))
            {
                model.LoadErrorMessage = connectionError;
            }
            else
            {
                ModelState.AddModelError(
                    string.Empty,
                    connectionError);
            }
        }

        return View(model);
    }

    private void AddApiValidationErrors(
        IReadOnlyDictionary<string, string[]> fieldErrors,
        HashSet<string> allowedFields)
    {
        foreach (var (field, messages) in fieldErrors)
        {
            var modelField = NormalizeApiField(
                field,
                allowedFields);

            foreach (var message in messages)
            {
                ModelState.AddModelError(modelField, message);
            }
        }
    }

    private static string NormalizeApiField(
        string field,
        HashSet<string> allowedFields)
    {
        var candidate = field
            .TrimStart('$', '.')
            .Split('.')
            .LastOrDefault() ?? string.Empty;

        return allowedFields.TryGetValue(
            candidate,
            out var modelField)
            ? modelField
            : string.Empty;
    }

    private static EditAdminUserViewModel MapEditUser(
        UserListItemApiResponse user)
    {
        var model = new EditAdminUserViewModel
        {
            FullName = user.FullName,
            Email = user.Email,
            StudentCode = user.StudentCode,
            PhoneNumber = user.PhoneNumber
        };

        DecorateEditUser(model, user);

        return model;
    }

    private static void DecorateEditUser(
        EditAdminUserViewModel model,
        UserListItemApiResponse user)
    {
        model.Id = user.Id;
        model.Role = user.Role;
        model.IsActive = user.IsActive;
        model.CreatedAt = DateTime.SpecifyKind(
            user.CreatedAt,
            DateTimeKind.Utc);
        model.CreatedAtLocal = TimeZoneInfo.ConvertTime(
            new DateTimeOffset(model.CreatedAt),
            BusinessTimeZone);
    }

    private async Task<IActionResult> EndInvalidSessionAsync(
        string message)
    {
        var returnUrl =
            $"{Request.PathBase}{Request.Path}{Request.QueryString}";

        HttpContext.Session.Clear();
        await HttpContext.SignOutAsync(
            CookieAuthenticationDefaults.AuthenticationScheme);

        TempData["AuthenticationError"] = message;

        return RedirectToAction(
            nameof(AccountController.Login),
            "Account",
            new { returnUrl });
    }

    private static AdminUsersViewModel CreateViewModel(
        string? search,
        string? role,
        bool? isActive,
        int page,
        int pageSize,
        bool isForbidden = false,
        string? errorMessage = null)
    {
        return new AdminUsersViewModel
        {
            Search = search,
            Role = role,
            IsActive = isActive,
            Page = page,
            PageSize = pageSize,
            IsForbidden = isForbidden,
            ErrorMessage = errorMessage
        };
    }

    private static AdminUserListItemViewModel MapUser(
        UserListItemApiResponse user)
    {
        var createdAtUtc = DateTime.SpecifyKind(
            user.CreatedAt,
            DateTimeKind.Utc);

        return new AdminUserListItemViewModel
        {
            Id = user.Id,
            Initials = GetInitials(user.FullName),
            FullName = user.FullName,
            Email = user.Email,
            StudentCode = user.StudentCode,
            PhoneNumber = user.PhoneNumber,
            Role = user.Role,
            RoleLabel = GetRoleLabel(user.Role),
            RoleCssClass = user.Role.ToLowerInvariant(),
            IsActive = user.IsActive,
            CreatedAtUtc = createdAtUtc,
            CreatedAtLocal = TimeZoneInfo.ConvertTime(
                new DateTimeOffset(createdAtUtc),
                BusinessTimeZone)
        };
    }

    private static string? NormalizeSearch(string? search)
    {
        if (string.IsNullOrWhiteSpace(search))
        {
            return null;
        }

        var normalized = search.Trim();

        return normalized.Length <= 150
            ? normalized
            : normalized[..150];
    }

    private static string? NormalizeRole(string? role)
    {
        if (string.IsNullOrWhiteSpace(role))
        {
            return null;
        }

        return AllowedRoles.FirstOrDefault(
            allowedRole => string.Equals(
                allowedRole,
                role.Trim(),
                StringComparison.OrdinalIgnoreCase));
    }

    private static string GetRoleLabel(string role)
    {
        return role switch
        {
            "Admin" => "Quản trị viên",
            "Staff" => "Cán bộ",
            "Organizer" => "Nhà tổ chức",
            "Student" => "Sinh viên",
            _ => role
        };
    }

    private static string GetInitials(string fullName)
    {
        var parts = fullName.Split(
            ' ',
            StringSplitOptions.RemoveEmptyEntries |
            StringSplitOptions.TrimEntries);

        if (parts.Length == 0)
        {
            return "SC";
        }

        var initials = parts.Length == 1
            ? parts[0][..1]
            : $"{parts[0][0]}{parts[^1][0]}";

        return initials.ToUpperInvariant();
    }

    private static TimeZoneInfo ResolveBusinessTimeZone()
    {
        foreach (var timeZoneId in new[]
        {
            "Asia/Ho_Chi_Minh",
            "SE Asia Standard Time"
        })
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(
                    timeZoneId);
            }
            catch (TimeZoneNotFoundException)
            {
            }
            catch (InvalidTimeZoneException)
            {
            }
        }

        return TimeZoneInfo.CreateCustomTimeZone(
            "SCEAMS Business Time",
            TimeSpan.FromHours(7),
            "SCEAMS Business Time",
            "SCEAMS Business Time");
    }
}
