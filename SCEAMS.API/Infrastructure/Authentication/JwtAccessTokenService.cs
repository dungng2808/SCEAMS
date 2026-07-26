using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SCEAMS.Application.DTOs;
using SCEAMS.Application.Interfaces;
using SCEAMS.Domain.Entities;

namespace SCEAMS.Infrastructure.Authentication;

public sealed class JwtAccessTokenService : IAccessTokenService
{
    private readonly JwtOptions _options;
    private readonly TimeProvider _timeProvider;

    public JwtAccessTokenService(
        IOptions<JwtOptions> options,
        TimeProvider timeProvider)
    {
        _options = options.Value;
        _timeProvider = timeProvider;
    }

    public GeneratedAccessToken Create(User user)
    {
        var issuedAtUtc = _timeProvider.GetUtcNow().UtcDateTime;
        var expiresAtUtc = issuedAtUtc.AddMinutes(
            _options.AccessTokenMinutes);
        var securityKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_options.SigningKey));
        var credentials = new SigningCredentials(
            securityKey,
            SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(
                JwtRegisteredClaimNames.Sub,
                user.Id.ToString()),
            new Claim(
                JwtRegisteredClaimNames.Email,
                user.Email),
            new Claim(
                "role",
                user.Role.ToString()),
            new Claim(
                JwtRegisteredClaimNames.Jti,
                Guid.NewGuid().ToString("N"))
        };

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: issuedAtUtc,
            expires: expiresAtUtc,
            signingCredentials: credentials);

        return new GeneratedAccessToken(
            Value: new JwtSecurityTokenHandler().WriteToken(token),
            ExpiresAtUtc: expiresAtUtc);
    }
}
