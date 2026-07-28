using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SCEAMS.MVC.Models.Api;
using SCEAMS.MVC.Services.ApiClients;
using SCEAMS.MVC.ViewModels;

namespace SCEAMS.MVC.Controllers;

[Route("Chatbot")]
[Authorize(Roles = "Student")]
public sealed class ChatbotController : Controller
{
    private readonly IEventFaqApiClient _eventFaqApiClient;
    private readonly ILogger<ChatbotController> _logger;

    public ChatbotController(
        IEventFaqApiClient eventFaqApiClient,
        ILogger<ChatbotController> logger)
    {
        _eventFaqApiClient = eventFaqApiClient;
        _logger = logger;
    }

    [HttpGet("")]
    public IActionResult Index()
    {
        return View(new ChatbotViewModel());
    }

    [HttpPost("")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(
        ChatbotViewModel model,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var result = await _eventFaqApiClient.AskAsync(
                new EventFaqRetrievalApiRequest(model.Question.Trim()),
                cancellationToken);

            if (result.IsUnauthorized)
            {
                return await EndInvalidSessionAsync();
            }

            if (result.IsForbidden)
            {
                return RedirectToAction(
                    nameof(AccountController.AccessDenied),
                    "Account");
            }

            if (!result.IsSuccess)
            {
                return View(new ChatbotViewModel
                {
                    Question = model.Question,
                    HasSearched = true,
                    ErrorMessage = result.ErrorMessage ??
                        "Không thể truy vấn Event lúc này.",
                    RateLimitUntilUtc = result.RetryAfterSeconds is > 0
                        ? DateTimeOffset.UtcNow.AddSeconds(result.RetryAfterSeconds.Value)
                        : null
                });
            }

            return View(new ChatbotViewModel
            {
                Question = model.Question,
                HasSearched = true,
                Answer = result.Answer,
                RelatedEvents = result.Events.Select(MapEvent).ToList()
            });
        }
        catch (Exception exception) when (
            exception is HttpRequestException or
            TaskCanceledException or
            System.Text.Json.JsonException)
        {
            _logger.LogWarning(exception, "Unable to retrieve FAQ events from API.");
            return View(new ChatbotViewModel
            {
                Question = model.Question,
                HasSearched = true,
                ErrorMessage = exception is TaskCanceledException
                    ? "Trợ lý phản hồi quá lâu. Vui lòng thử lại sau ít phút."
                    : "Không thể kết nối tới API. Hãy kiểm tra API đang chạy và thử lại."
            });
        }
    }

    [HttpGet("History")]
    public async Task<IActionResult> History(
        int page = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var normalizedPage = Math.Max(page, 1);
        var normalizedPageSize = Math.Clamp(pageSize, 1, 50);
        try
        {
            var result = await _eventFaqApiClient.GetHistoryAsync(
                normalizedPage,
                normalizedPageSize,
                cancellationToken);
            if (result.IsUnauthorized)
            {
                return await EndInvalidSessionAsync();
            }

            if (result.IsForbidden)
            {
                return RedirectToAction(
                    nameof(AccountController.AccessDenied),
                    "Account");
            }

            if (!result.IsSuccess || result.Page is null)
            {
                return View(new ChatHistoryViewModel
                {
                    Page = normalizedPage,
                    PageSize = normalizedPageSize,
                    ErrorMessage = result.ErrorMessage ??
                        "Không thể tải lịch sử chatbot."
                });
            }

            return View(MapHistory(result.Page));
        }
        catch (Exception exception) when (
            exception is HttpRequestException or
            TaskCanceledException or
            System.Text.Json.JsonException)
        {
            _logger.LogWarning(exception, "Unable to load chatbot history from API.");
            return View(new ChatHistoryViewModel
            {
                Page = normalizedPage,
                PageSize = normalizedPageSize,
                ErrorMessage = "Không thể kết nối tới API. Vui lòng thử lại sau."
            });
        }
    }

    private async Task<IActionResult> EndInvalidSessionAsync()
    {
        await HttpContext.SignOutAsync(
            CookieAuthenticationDefaults.AuthenticationScheme);
        HttpContext.Session.Clear();
        var returnUrl = Request.PathBase + Request.Path + Request.QueryString;
        return RedirectToAction(
            nameof(AccountController.Login),
            "Account",
            new { returnUrl });
    }

    private static ChatbotEventViewModel MapEvent(EventFaqEventApiResponse item)
    {
        return new ChatbotEventViewModel
        {
            Id = item.Id,
            Title = item.Title,
            ClubName = item.ClubName,
            VenueName = item.VenueName,
            StartTime = item.StartTime,
            EndTime = item.EndTime,
            Capacity = item.Capacity,
            RegisteredCount = item.RegisteredCount,
            SlotsRemaining = item.SlotsRemaining
        };
    }

    private static ChatHistoryViewModel MapHistory(ChatHistoryPageApiResponse page)
    {
        return new ChatHistoryViewModel
        {
            Page = page.Page,
            PageSize = page.PageSize,
            TotalItems = page.TotalItems,
            TotalPages = page.TotalPages,
            HasPreviousPage = page.HasPreviousPage,
            HasNextPage = page.HasNextPage,
            Items = page.Items.Select(item => new ChatHistoryItemViewModel
            {
                Id = item.Id,
                Question = item.Question,
                AnswerText = item.AnswerText,
                RelatedEventIds = item.RelatedEventIds,
                CreatedAt = item.CreatedAt
            }).ToList()
        };
    }
}
