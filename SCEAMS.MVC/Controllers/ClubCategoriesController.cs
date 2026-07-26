using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using SCEAMS.MVC.Models.Api;
using SCEAMS.MVC.Services.ApiClients;
using SCEAMS.MVC.ViewModels;

namespace SCEAMS.MVC.Controllers;

[Route("ClubCategories")]
public sealed class ClubCategoriesController : Controller
{
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
                return View(new ClubCategoriesViewModel
                {
                    CanManage = User.IsInRole("Admin"),
                    ErrorMessage = result.ErrorMessage ??
                        "Không thể tải danh mục câu lạc bộ."
                });
            }

            return View(new ClubCategoriesViewModel
            {
                CanManage = User.IsInRole("Admin"),
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
}
