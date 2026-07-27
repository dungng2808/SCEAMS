using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SCEAMS.MVC.ViewModels;

namespace SCEAMS.MVC.Controllers;

[Route("Errors")]
[AllowAnonymous]
public sealed class ErrorsController : Controller
{
    [HttpGet("Api/{statusCode:int}")]
    public IActionResult Api(
        int statusCode,
        string? detail,
        string? traceId,
        string? returnUrl)
    {
        if (statusCode == StatusCodes.Status401Unauthorized)
        {
            var safeReturnUrl = Url.IsLocalUrl(returnUrl) ? returnUrl : "/";
            return RedirectToAction("Login", "Account", new { returnUrl = safeReturnUrl });
        }

        if (statusCode == StatusCodes.Status403Forbidden)
        {
            return RedirectToAction("AccessDenied", "Account");
        }

        return View(new ApiErrorViewModel
        {
            StatusCode = statusCode,
            Title = GetTitle(statusCode),
            Detail = string.IsNullOrWhiteSpace(detail)
                ? "API không thể hoàn thành yêu cầu này."
                : detail,
            TraceId = traceId,
            ReturnUrl = Url.IsLocalUrl(returnUrl) ? returnUrl : null
        });
    }

    private static string GetTitle(int statusCode) => statusCode switch
    {
        StatusCodes.Status400BadRequest => "Dữ liệu yêu cầu chưa hợp lệ",
        StatusCodes.Status404NotFound => "Không tìm thấy tài nguyên",
        StatusCodes.Status409Conflict => "Yêu cầu đang xung đột",
        StatusCodes.Status406NotAcceptable => "Định dạng response không được hỗ trợ",
        StatusCodes.Status500InternalServerError => "API đang gặp sự cố",
        _ => "Yêu cầu chưa thể hoàn thành"
    };
}
