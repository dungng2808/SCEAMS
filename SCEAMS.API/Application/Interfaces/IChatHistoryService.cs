using System.Security.Claims;
using SCEAMS.Application.Common;
using SCEAMS.Application.DTOs.Chatbot;

namespace SCEAMS.Application.Interfaces;

public interface IChatHistoryService
{
    Task<Result> SaveAsync(
        string question,
        string answer,
        IReadOnlyList<int> relatedEventIds,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default);

    Task<Result<ChatHistoryPageDto>> GetForCurrentStudentAsync(
        ClaimsPrincipal user,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}
