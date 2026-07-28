using Microsoft.AspNetCore.Http;

namespace SCEAMS.Application.Common;

public class Result
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public int StatusCode { get; init; }
    public object? ErrorData { get; init; }

    public static Result Ok(string message = "Success") => new()
    {
        Success = true,
        Message = message,
        StatusCode = StatusCodes.Status200OK
    };

    public static Result NoContent(string message = "No content") => new()
    {
        Success = true,
        Message = message,
        StatusCode = StatusCodes.Status204NoContent
    };

    public static Result Fail(string message, int statusCode = StatusCodes.Status400BadRequest) => new()
    {
        Success = false,
        Message = message,
        StatusCode = statusCode
    };

    public static Result Fail(
        string message,
        int statusCode,
        object errorData) => new()
    {
        Success = false,
        Message = message,
        StatusCode = statusCode,
        ErrorData = errorData
    };
}

public class Result<T> : Result
{
    public T? Data { get; init; }

    public static Result<T> Ok(T data, string message = "Success") => new()
    {
        Success = true,
        Message = message,
        StatusCode = StatusCodes.Status200OK,
        Data = data
    };

    public static Result<T> Created(T data, string message = "Created") => new()
    {
        Success = true,
        Message = message,
        StatusCode = StatusCodes.Status201Created,
        Data = data
    };

    public new static Result<T> Fail(string message, int statusCode = StatusCodes.Status400BadRequest) => new()
    {
        Success = false,
        Message = message,
        StatusCode = statusCode,
        Data = default
    };

    public new static Result<T> Fail(
        string message,
        int statusCode,
        object errorData) => new()
    {
        Success = false,
        Message = message,
        StatusCode = statusCode,
        ErrorData = errorData,
        Data = default
    };
}
