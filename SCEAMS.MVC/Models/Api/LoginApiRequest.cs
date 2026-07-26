namespace SCEAMS.MVC.Models.Api;

public sealed record LoginApiRequest(
    string Email,
    string Password);
