using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace SCEAMS.Api.Middleware;

public sealed class ApiExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ApiExceptionHandlingMiddleware> _logger;

    public ApiExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ApiExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception exception) when (!context.RequestAborted.IsCancellationRequested)
        {
            _logger.LogError(
                exception,
                "Unhandled API exception for {Method} {Path}; trace {TraceId}.",
                context.Request.Method,
                context.Request.Path,
                context.TraceIdentifier);
            var mapped = Map(exception);
            await ProblemDetailsWriter.WriteAsync(
                context,
                mapped.StatusCode,
                mapped.Title,
                mapped.Detail);
        }
    }

    private static (int StatusCode, string Title, string Detail) Map(
        Exception exception) => exception switch
        {
            ValidationException validation => (
                StatusCodes.Status400BadRequest,
                "Validation failed",
                validation.Message),
            ArgumentException argument => (
                StatusCodes.Status400BadRequest,
                "Bad request",
                argument.Message),
            JsonException => (
                StatusCodes.Status400BadRequest,
                "Invalid JSON",
                "Request body không đúng định dạng JSON."),
            UnauthorizedAccessException => (
                StatusCodes.Status403Forbidden,
                "Forbidden",
                "Bạn không có quyền thực hiện thao tác này."),
            DbUpdateException => (
                StatusCodes.Status409Conflict,
                "Conflict",
                "Dữ liệu không thể ghi vì đang xung đột với trạng thái hiện tại."),
            KeyNotFoundException => (
                StatusCodes.Status404NotFound,
                "Not found",
                "Không tìm thấy tài nguyên được yêu cầu."),
            _ => (
                StatusCodes.Status500InternalServerError,
                "Internal server error",
                "Đã xảy ra lỗi ngoài dự kiến. Vui lòng thử lại sau.")
        };
}
