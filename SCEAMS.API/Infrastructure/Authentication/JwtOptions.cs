namespace SCEAMS.Infrastructure.Authentication;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; init; } = string.Empty;
    public string Audience { get; init; } = string.Empty;
    public string SigningKey { get; init; } = string.Empty;
    public int AccessTokenMinutes { get; init; } = 60;

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Issuer))
        {
            throw new InvalidOperationException(
                "Jwt:Issuer is not configured.");
        }

        if (string.IsNullOrWhiteSpace(Audience))
        {
            throw new InvalidOperationException(
                "Jwt:Audience is not configured.");
        }

        if (SigningKey.Length < 32)
        {
            throw new InvalidOperationException(
                "Jwt:SigningKey must contain at least 32 characters. " +
                "Store it in .NET User Secrets or an environment variable.");
        }

        if (AccessTokenMinutes is < 1 or > 1440)
        {
            throw new InvalidOperationException(
                "Jwt:AccessTokenMinutes must be between 1 and 1440.");
        }
    }
}
