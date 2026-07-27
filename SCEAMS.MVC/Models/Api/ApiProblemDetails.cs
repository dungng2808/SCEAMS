namespace SCEAMS.MVC.Models.Api;

public sealed class ApiProblemDetails
{
    public string? Type { get; init; }
    public string? Title { get; init; }
    public int? Status { get; init; }
    public string? Detail { get; init; }
    public string? Instance { get; init; }
    public string? TraceId { get; init; }
    public string? Message { get; init; }
}
