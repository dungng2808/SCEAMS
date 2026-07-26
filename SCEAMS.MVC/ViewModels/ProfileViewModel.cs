namespace SCEAMS.MVC.ViewModels;

public sealed class ProfileViewModel
{
    public ProfileDetailsViewModel? Profile { get; init; }
    public string? ErrorMessage { get; init; }
}

public sealed class ProfileDetailsViewModel
{
    public int Id { get; init; }
    public string Initials { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string? StudentCode { get; init; }
    public string? PhoneNumber { get; init; }
    public string Role { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public DateTime CreatedAtUtc { get; init; }
    public DateTimeOffset CreatedAtLocal { get; init; }
}
