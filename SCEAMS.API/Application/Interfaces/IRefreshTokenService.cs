using SCEAMS.Application.DTOs;

namespace SCEAMS.Application.Interfaces;

public interface IRefreshTokenService
{
    GeneratedRefreshToken Create();
    string ComputeHash(string token);
}
