namespace SCEAMS.MVC.ViewModels;

public sealed class ApiErrorViewModel
{
    public int StatusCode { get; init; }
    public string Title { get; init; } = "Yêu cầu chưa thể hoàn thành";
    public string Detail { get; init; } = "Vui lòng thử lại sau.";
    public string? TraceId { get; init; }
    public string? ReturnUrl { get; init; }
}
