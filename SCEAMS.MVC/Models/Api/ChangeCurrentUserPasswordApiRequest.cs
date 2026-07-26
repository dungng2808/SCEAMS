namespace SCEAMS.MVC.Models.Api;

public sealed record ChangeCurrentUserPasswordApiRequest(
    string CurrentPassword,
    string NewPassword,
    string ConfirmPassword);
