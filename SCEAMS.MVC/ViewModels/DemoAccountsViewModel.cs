namespace SCEAMS.MVC.ViewModels;

public sealed class DemoAccountsViewModel
{
    public bool DatabaseOnline { get; init; }
    public bool DemoSeedReady { get; init; }
    public int ExistingDemoAccountCount { get; init; }
    public string? ErrorMessage { get; init; }

    public IReadOnlyList<DemoAccountViewModel> Accounts { get; init; } =
    [
        new("Admin", "admin@sceams.edu.vn"),
        new("Staff", "staff@sceams.edu.vn"),
        new("Organizer", "organizer@sceams.edu.vn"),
        new("Student", "student@sceams.edu.vn")
    ];
}

public sealed record DemoAccountViewModel(
    string Role,
    string Email);
