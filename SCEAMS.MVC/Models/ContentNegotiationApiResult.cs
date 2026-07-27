namespace SCEAMS.MVC.Models;

public sealed class ContentNegotiationApiResult
{
    public int StatusCode { get; init; }
    public string StatusDescription { get; init; } = string.Empty;
    public string ContentType { get; init; } = string.Empty;
    public string RawResponse { get; init; } = string.Empty;
    public bool IsSuccess => StatusCode is >= 200 and < 300;
    public bool IsNotAcceptable => StatusCode == StatusCodes.Status406NotAcceptable;
}
