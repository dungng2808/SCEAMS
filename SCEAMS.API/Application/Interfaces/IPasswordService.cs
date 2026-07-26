using SCEAMS.Domain.Entities;

namespace SCEAMS.Application.Interfaces;

public interface IPasswordService
{
    string HashPassword(User user, string password);

    bool VerifyPassword(
        User user,
        string passwordHash,
        string providedPassword);
}
