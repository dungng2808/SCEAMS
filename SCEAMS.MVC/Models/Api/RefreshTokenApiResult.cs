namespace SCEAMS.MVC.Models.Api;

public sealed class RefreshTokenApiResult
{
    public bool IsSuccess { get; init; }
    public RefreshTokenApiResponse? Response { get; init; }
}
