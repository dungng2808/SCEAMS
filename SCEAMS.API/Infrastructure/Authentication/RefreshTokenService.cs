using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using SCEAMS.Application.DTOs;
using SCEAMS.Application.Interfaces;

namespace SCEAMS.Infrastructure.Authentication;

public sealed class RefreshTokenService : IRefreshTokenService
{
    private const int TokenByteLength = 64;

    private readonly JwtOptions _options;
    private readonly TimeProvider _timeProvider;

    public RefreshTokenService(
        IOptions<JwtOptions> options,
        TimeProvider timeProvider)
    {
        _options = options.Value;
        _timeProvider = timeProvider;
    }

    public GeneratedRefreshToken Create()
    {
        var tokenBytes = RandomNumberGenerator.GetBytes(
            TokenByteLength);
        var value = Convert.ToBase64String(tokenBytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
        var expiresAtUtc = _timeProvider
            .GetUtcNow()
            .UtcDateTime
            .AddDays(_options.RefreshTokenDays);

        return new GeneratedRefreshToken(
            Value: value,
            Hash: ComputeHash(value),
            ExpiresAtUtc: expiresAtUtc);
    }

    public string ComputeHash(string token)
    {
        var tokenBytes = Encoding.UTF8.GetBytes(token);
        return Convert.ToHexString(
            SHA256.HashData(tokenBytes));
    }
}
