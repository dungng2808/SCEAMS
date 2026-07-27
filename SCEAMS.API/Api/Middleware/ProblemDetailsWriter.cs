using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace SCEAMS.Api.Middleware;

public static class ProblemDetailsWriter
{
    public static async Task WriteAsync(
        HttpContext context,
        int statusCode,
        string title,
        string detail,
        string? type = null)
    {
        if (context.Response.HasStarted ||
            !string.IsNullOrWhiteSpace(context.Response.ContentType))
        {
            return;
        }

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";
        var payload = new
        {
            type = type ?? $"https://httpstatuses.com/{statusCode}",
            title,
            status = statusCode,
            detail,
            instance = context.Request.Path.Value,
            traceId = context.TraceIdentifier
        };
        await context.Response.WriteAsync(JsonSerializer.Serialize(payload));
    }
}
