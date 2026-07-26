using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SCEAMS.MVC.Models.Api;
using SCEAMS.MVC.Services.ApiClients;
using SCEAMS.MVC.ViewModels;

namespace SCEAMS.MVC.Controllers;

[Route("ClubCategories")]
public sealed class ClubCategoriesController : Controller
{
    private static readonly HashSet<string> CreateCategoryFields =
        new(
        [
            nameof(CreateClubCategoryViewModel.Name),
            nameof(CreateClubCategoryViewModel.Description)
        ],
        StringComparer.OrdinalIgnoreCase);

    private static readonly string[] Themes =
        ["blue", "violet", "amber", "emerald"];

    private readonly IClubCategoryApiClient _clubCategoryApiClient;
    private readonly ILogger<ClubCategoriesController> _logger;

    public ClubCategoriesController(
        IClubCategoryApiClient clubCategoryApiClient,
        ILogger<ClubCategoriesController> logger)
    {
        _clubCategoryApiClient = clubCategoryApiClient;
        _logger = logger;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _clubCategoryApiClient
                .GetClubCategoriesAsync(cancellationToken);

            if (!result.IsSuccess)
            {
                if (result.IsUnauthorized &&
                    User.Identity?.IsAuthenticated == true)
                {
                    return await EndInvalidSessionAsync(
                        result.ErrorMessage ??
                        "Phiên đăng nhập không còn hợp lệ.");
                }

                return View(new ClubCategoriesViewModel
                {
                    CanManage = User.IsInRole("Admin"),
                    ErrorMessage = result.ErrorMessage ??
                        "Không thể tải danh mục câu lạc bộ."
                });
            }

            var highlightedCategoryId =
                GetHighlightedCategoryId();

            if (highlightedCategoryId is int createdCategoryId &&
                !result.Categories.Any(
                    category => category.Id == createdCategoryId))
            {
                highlightedCategoryId = null;
                TempData.Remove("CategoryCreatedSuccess");
            }

            return View(new ClubCategoriesViewModel
            {
                CanManage = User.IsInRole("Admin"),
                HighlightedCategoryId =
                    highlightedCategoryId,
                Categories = result.Categories
                    .Select(MapCategory)
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
                "Unable to load club categories from the API.");

            return View(new ClubCategoriesViewModel
            {
                CanManage = User.IsInRole("Admin"),
                ErrorMessage =
                    "Không thể kết nối tới API. Vui lòng thử lại sau."
            });
        }
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("Create")]
    public IActionResult Create()
    {
        return View(new CreateClubCategoryViewModel());
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        CreateClubCategoryViewModel model,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var result = await _clubCategoryApiClient
                .CreateClubCategoryAsync(
                    new CreateClubCategoryApiRequest(
                        model.Name,
                        model.Description),
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

            if (result.IsSuccess && result.Category is not null)
            {
                TempData["CategoryCreatedSuccess"] =
                    $"Đã tạo danh mục “{result.Category.Name}” thành công.";
                TempData["CategoryCreatedId"] =
                    result.Category.Id.ToString();

                return RedirectToAction(nameof(Index));
            }

            AddApiValidationErrors(result.FieldErrors);

            if (result.FieldErrors.Count == 0)
            {
                ModelState.AddModelError(
                    string.Empty,
                    result.ErrorMessage ??
                    "Không thể tạo danh mục vào lúc này.");
            }
        }
        catch (Exception exception) when (
            exception is HttpRequestException or
            TaskCanceledException or
            JsonException)
        {
            _logger.LogWarning(
                exception,
                "Unable to create a club category from MVC.");

            ModelState.AddModelError(
                string.Empty,
                "Không thể kết nối tới API. Vui lòng thử lại sau.");
        }

        return View(model);
    }

    private static ClubCategoryListItemViewModel MapCategory(
        ClubCategoryApiResponse category,
        int index)
    {
        return new ClubCategoryListItemViewModel
        {
            Id = category.Id,
            SequenceNumber = index + 1,
            Name = category.Name,
            Description = category.Description,
            Initials = GetInitials(category.Name),
            Theme = Themes[index % Themes.Length]
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
            _ => string.Concat(
                words[0][0],
                words[^1][0])
                .ToUpperInvariant()
        };
    }

    private int? GetHighlightedCategoryId()
    {
        return int.TryParse(
            TempData["CategoryCreatedId"] as string,
            out var categoryId)
            ? categoryId
            : null;
    }

    private void AddApiValidationErrors(
        IReadOnlyDictionary<string, string[]> fieldErrors)
    {
        foreach (var (field, messages) in fieldErrors)
        {
            var candidate = field
                .TrimStart('$', '.')
                .Split('.')
                .LastOrDefault() ?? string.Empty;
            var modelField = CreateCategoryFields.TryGetValue(
                candidate,
                out var allowedField)
                ? allowedField
                : string.Empty;

            foreach (var message in messages)
            {
                ModelState.AddModelError(modelField, message);
            }
        }
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
}
