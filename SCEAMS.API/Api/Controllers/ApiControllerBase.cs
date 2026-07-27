using Microsoft.AspNetCore.Mvc;
using SCEAMS.Application.Common;

namespace SCEAMS.Api.Controllers;

[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    protected IActionResult ToActionResult(Result result)
    {
        if (result.Success &&
            result.StatusCode == StatusCodes.Status204NoContent)
        {
            return NoContent();
        }

        return StatusCode(
            result.StatusCode,
            BuildErrorPayload(result));
    }

    protected IActionResult ToActionResult<T>(Result<T> result)
    {
        if (result.Success)
        {
            return StatusCode(result.StatusCode, result.Data);
        }

        return StatusCode(
            result.StatusCode,
            BuildErrorPayload(result));
    }

    private object BuildErrorPayload(Result result)
    {
        var problem = new ProblemDetails
        {
            Status = result.StatusCode,
            Title = GetTitle(result.StatusCode),
            Detail = result.Message,
            Instance = HttpContext.Request.Path
        };
        problem.Extensions["traceId"] = HttpContext.TraceIdentifier;
        if (result.ErrorData is not null)
        {
            problem.Extensions["conflicts"] = result.ErrorData;
        }

        return problem;
    }

    private static string GetTitle(int statusCode) => statusCode switch
    {
        StatusCodes.Status400BadRequest => "Bad request",
        StatusCodes.Status401Unauthorized => "Unauthorized",
        StatusCodes.Status403Forbidden => "Forbidden",
        StatusCodes.Status404NotFound => "Not found",
        StatusCodes.Status409Conflict => "Conflict",
        _ => "Request failed"
    };
}
