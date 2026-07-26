using SCEAMS.Application.DTOs;
using SCEAMS.Domain.Entities;

namespace SCEAMS.Application.Interfaces;

public interface IAccessTokenService
{
    GeneratedAccessToken Create(User user);
}
