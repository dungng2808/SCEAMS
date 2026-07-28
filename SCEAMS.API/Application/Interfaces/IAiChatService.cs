using System.Security.Claims;
using SCEAMS.Application.Common;
using SCEAMS.Application.DTOs.Chatbot;

namespace SCEAMS.Application.Interfaces;

public interface IAiChatService
{
    Task<Result<AiChatResponseDto>> AskAsync(
        AiChatRequestDto request,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default);
}
