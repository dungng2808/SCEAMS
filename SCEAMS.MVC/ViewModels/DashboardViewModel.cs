namespace SCEAMS.MVC.ViewModels;

public sealed class DashboardViewModel
{
    public string FullName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Role { get; init; } = string.Empty;
    public string Heading { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string NextStep { get; init; } = string.Empty;
    public string PrimaryActionText { get; init; } = string.Empty;
    public string PrimaryController { get; init; } = string.Empty;
    public string PrimaryAction { get; init; } = string.Empty;
}
