namespace SCEAMS.MVC.ViewModels;

public sealed class ContentNegotiationViewModel
{
    public string Format { get; init; } = "json";
    public int Top { get; init; } = 10;
    public ContentNegotiationResponseViewModel? Response { get; init; }
    public string? ErrorMessage { get; init; }
}

public sealed class ContentNegotiationResponseViewModel
{
    public int StatusCode { get; init; }
    public string StatusDescription { get; init; } = string.Empty;
    public string ContentType { get; init; } = string.Empty;
    public string RawResponse { get; init; } = string.Empty;
    public bool IsSuccess => StatusCode is >= 200 and < 300;
    public bool IsNotAcceptable => StatusCode == 406;
}
