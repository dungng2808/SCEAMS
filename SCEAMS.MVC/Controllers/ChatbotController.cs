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
            var result = await _eventFaqApiClient.RetrieveEventsAsync(
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
                        "Không thể truy vấn Event lúc này."
                });
            }

            return View(new ChatbotViewModel
            {
                Question = model.Question,
                HasSearched = true,
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
                ErrorMessage = "Không thể kết nối tới API. Hãy kiểm tra API đang chạy và thử lại."
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
}
