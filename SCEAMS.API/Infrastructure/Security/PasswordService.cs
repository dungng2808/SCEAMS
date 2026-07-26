using Microsoft.AspNetCore.Identity;
using SCEAMS.Application.Interfaces;
using SCEAMS.Domain.Entities;

namespace SCEAMS.Infrastructure.Security;

public sealed class PasswordService : IPasswordService
{
    private readonly IPasswordHasher<User> _passwordHasher;

    public PasswordService(IPasswordHasher<User> passwordHasher)
    {
        _passwordHasher = passwordHasher;
    }

    public string HashPassword(User user, string password)
    {
        return _passwordHasher.HashPassword(user, password);
    }

    public bool VerifyPassword(
        User user,
        string passwordHash,
        string providedPassword)
    {
        var result = _passwordHasher.VerifyHashedPassword(
            user,
            passwordHash,
            providedPassword);

        return result is PasswordVerificationResult.Success or
            PasswordVerificationResult.SuccessRehashNeeded;
    }
}
