namespace SCEAMS.Infrastructure.Data.Seed;

public sealed class SeedDataOptions
{
    public const string SectionName = "SeedData";

    public string AdminPassword { get; init; } = string.Empty;
    public string StaffPassword { get; init; } = string.Empty;
    public string OrganizerPassword { get; init; } = string.Empty;
    public string StudentPassword { get; init; } = string.Empty;

    public void Validate()
    {
        var missingKeys = new List<string>();

        AddMissingKey(
            missingKeys,
            nameof(AdminPassword),
            AdminPassword);
        AddMissingKey(
            missingKeys,
            nameof(StaffPassword),
            StaffPassword);
        AddMissingKey(
            missingKeys,
            nameof(OrganizerPassword),
            OrganizerPassword);
        AddMissingKey(
            missingKeys,
            nameof(StudentPassword),
            StudentPassword);

        if (missingKeys.Count > 0)
        {
            throw new InvalidOperationException(
                $"Missing seed password configuration: {string.Join(", ", missingKeys)}. " +
                "Use environment variables or .NET User Secrets.");
        }
    }

    private static void AddMissingKey(
        ICollection<string> missingKeys,
        string key,
        string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            missingKeys.Add(key);
        }
    }
}
