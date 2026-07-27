using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using SCEAMS.MVC.Services.ApiClients;
using SCEAMS.MVC.ViewModels;

namespace SCEAMS.MVC.Controllers;

[Route("System")]
public sealed class SystemController : Controller
{
    private readonly IHealthApiClient _healthApiClient;
    private readonly IContentNegotiationApiClient _contentNegotiationApiClient;
    private readonly INotificationLogApiClient _notificationLogApiClient;
    private readonly IEventReminderApiClient _eventReminderApiClient;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<SystemController> _logger;

    public SystemController(
        IHealthApiClient healthApiClient,
        IContentNegotiationApiClient contentNegotiationApiClient,
        INotificationLogApiClient notificationLogApiClient,
        IEventReminderApiClient eventReminderApiClient,
        IWebHostEnvironment environment,
        ILogger<SystemController> logger)
    {
        _healthApiClient = healthApiClient;
        _contentNegotiationApiClient = contentNegotiationApiClient;
        _notificationLogApiClient = notificationLogApiClient;
        _eventReminderApiClient = eventReminderApiClient;
        _environment = environment;
        _logger = logger;
    }

    [HttpGet("Health")]
    public async Task<IActionResult> Health(
        CancellationToken cancellationToken)
    {
        var apiStatusTask = GetApiStatusAsync(cancellationToken);
        var databaseStatusTask = GetDatabaseStatusAsync(
            cancellationToken);

        await Task.WhenAll(apiStatusTask, databaseStatusTask);

        return View(new SystemHealthViewModel
        {
            Api = await apiStatusTask,
            Database = await databaseStatusTask,
            CheckedAt = DateTimeOffset.Now
        });
    }

    [HttpGet("DemoAccounts")]
    public async Task<IActionResult> DemoAccounts(
        CancellationToken cancellationToken)
    {
        if (!_environment.IsDevelopment())
        {
            return NotFound();
        }

        try
        {
            var response = await _healthApiClient
                .GetDatabaseHealthAsync(cancellationToken);

            if (response is null)
            {
                throw new InvalidOperationException(
                    "The API returned an empty database health response.");
            }

            return View(new DemoAccountsViewModel
            {
                DatabaseOnline = response.CanConnect,
                DemoSeedReady = response.DemoSeedReady,
                ExistingDemoAccountCount = response.DemoAccountCount
            });
        }
        catch (Exception exception) when (
            exception is HttpRequestException or
            TaskCanceledException or
            InvalidOperationException)
        {
            _logger.LogWarning(
                exception,
                "Unable to verify the development demo accounts.");

            return View(new DemoAccountsViewModel
            {
                ErrorMessage = "Không thể xác nhận dữ liệu seed qua API. Hãy kiểm tra API, SQL Server và chạy lại lệnh seed."
            });
        }
    }

    [HttpGet("ContentNegotiation")]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> ContentNegotiation(
        string format = "json",
        int top = 10,
        CancellationToken cancellationToken = default)
    {
        if (!_environment.IsDevelopment())
        {
            return NotFound();
        }

        var normalizedFormat = format.Trim().ToLowerInvariant() switch
        {
            "xml" => "xml",
            "unsupported" => "unsupported",
            _ => "json"
        };
        var acceptMediaType = normalizedFormat switch
        {
            "xml" => "application/xml",
            "unsupported" => "text/csv",
            _ => "application/json"
        };

        try
        {
            var response = await _contentNegotiationApiClient.GetEventsAsync(
                acceptMediaType,
                top,
                cancellationToken);
            return View(new ContentNegotiationViewModel
            {
                Format = normalizedFormat,
                Top = Math.Clamp(top, 1, 50),
                Response = new ContentNegotiationResponseViewModel
                {
                    StatusCode = response.StatusCode,
                    StatusDescription = response.StatusDescription,
                    ContentType = response.ContentType,
                    RawResponse = response.RawResponse
                }
            });
        }
        catch (Exception exception) when (
            exception is HttpRequestException or TaskCanceledException)
        {
            _logger.LogWarning(exception, "Unable to run content negotiation demo.");
            return View(new ContentNegotiationViewModel
            {
                Format = normalizedFormat,
                Top = Math.Clamp(top, 1, 50),
                ErrorMessage = "Không thể kết nối tới API. Vui lòng kiểm tra API đang chạy."
            });
        }
    }

    [HttpGet("NotificationLog")]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> NotificationLog(
        int? eventId,
        string? notificationType,
        bool? success,
        CancellationToken cancellationToken = default)
    {
        if (!_environment.IsDevelopment())
        {
            return NotFound();
        }

        try
        {
            var result = await _notificationLogApiClient.GetLogsAsync(
                eventId,
                notificationType,
                success,
                cancellationToken);
            if (result.IsUnauthorized && User.Identity?.IsAuthenticated == true)
            {
                await HttpContext.SignOutAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme);
                HttpContext.Session.Clear();
                TempData["ErrorMessage"] = result.ErrorMessage;
                return RedirectToAction("Login", "Account", new { returnUrl = "/System/NotificationLog" });
            }

            if (result.IsForbidden)
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            return View(new NotificationLogViewModel
            {
                EventId = eventId,
                NotificationType = notificationType,
                Success = success,
                ErrorMessage = result.IsSuccess ? null : result.ErrorMessage,
                Entries = result.Entries
            });
        }
        catch (Exception exception) when (
            exception is HttpRequestException or TaskCanceledException or JsonException)
        {
            _logger.LogWarning(exception, "Unable to load notification logs.");
            return View(new NotificationLogViewModel
            {
                EventId = eventId,
                NotificationType = notificationType,
                Success = success,
                ErrorMessage = "Không thể kết nối tới API. Vui lòng thử lại sau."
            });
        }
    }

    [HttpPost("NotificationLog/RunReminder")]
    [Authorize(Roles = "Admin,Staff")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RunReminder(
        int? eventId,
        string? notificationType,
        bool? success,
        CancellationToken cancellationToken = default)
    {
        if (!_environment.IsDevelopment())
        {
            return NotFound();
        }

        try
        {
            var result = await _eventReminderApiClient.RunAsync(cancellationToken);
            if (result.IsUnauthorized && User.Identity?.IsAuthenticated == true)
            {
                await HttpContext.SignOutAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme);
                HttpContext.Session.Clear();
                TempData["ErrorMessage"] = result.ErrorMessage;
                return RedirectToAction("Login", "Account", new { returnUrl = "/System/NotificationLog" });
            }

            if (result.IsForbidden)
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            TempData[result.IsSuccess ? "SuccessMessage" : "NotificationLogError"] =
                result.IsSuccess && result.Summary is not null
                    ? $"Reminder job: quét {result.Summary.Scanned}, gửi {result.Summary.Sent}, bỏ qua {result.Summary.Skipped}, lỗi {result.Summary.Failed}."
                    : result.ErrorMessage ?? "Không thể chạy reminder job.";
        }
        catch (Exception exception) when (
            exception is HttpRequestException or TaskCanceledException or JsonException)
        {
            _logger.LogWarning(exception, "Unable to run reminder job from MVC.");
            TempData["NotificationLogError"] = "Không thể kết nối tới API. Vui lòng thử lại sau.";
        }

        return RedirectToAction(nameof(NotificationLog), new { eventId, notificationType, success });
    }

    private async Task<ApiHealthStatusViewModel> GetApiStatusAsync(
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await _healthApiClient.GetHealthAsync(
                cancellationToken);

            if (response is null)
            {
                throw new InvalidOperationException(
                    "The API returned an empty health response.");
            }

            return new ApiHealthStatusViewModel
            {
                IsOnline = true,
                Service = response.Service,
                Version = response.Version,
                Status = response.Status
            };
        }
        catch (Exception exception) when (
            exception is HttpRequestException or
            TaskCanceledException or
            InvalidOperationException)
        {
            _logger.LogWarning(
                exception,
                "Unable to retrieve the API health status.");

            return new ApiHealthStatusViewModel
            {
                IsOnline = false,
                Status = "Offline",
                ErrorMessage = "Không thể kết nối tới SCEAMS API. Hãy kiểm tra API đã được khởi động và cấu hình ApiSettings:BaseUrl."
            };
        }
    }

    private async Task<DatabaseHealthStatusViewModel>
        GetDatabaseStatusAsync(
            CancellationToken cancellationToken)
    {
        try
        {
            var response = await _healthApiClient
                .GetDatabaseHealthAsync(cancellationToken);

            if (response is null)
            {
                throw new InvalidOperationException(
                    "The API returned an empty database health response.");
            }

            return new DatabaseHealthStatusViewModel
            {
                IsOnline = response.CanConnect,
                DatabaseName = response.Database,
                Status = response.Status,
                DemoSeedReady = response.DemoSeedReady,
                DemoAccountCount = response.DemoAccountCount
            };
        }
        catch (Exception exception) when (
            exception is HttpRequestException or
            TaskCanceledException or
            InvalidOperationException)
        {
            _logger.LogWarning(
                exception,
                "Unable to retrieve the database health status.");

            return new DatabaseHealthStatusViewModel
            {
                IsOnline = false,
                Status = "Offline",
                ErrorMessage = "Không thể kết nối tới SQL Server. Hãy kiểm tra container, connection string và migration."
            };
        }
    }
}
