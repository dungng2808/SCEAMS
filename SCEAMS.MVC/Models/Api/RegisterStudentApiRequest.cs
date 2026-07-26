namespace SCEAMS.MVC.Models.Api;

public sealed record RegisterStudentApiRequest(
    string FullName,
    string Email,
    string StudentCode,
    string? PhoneNumber,
    string Password,
    string ConfirmPassword);
