using SCEAMS.Application.Common;
using SCEAMS.Application.DTOs.Chatbot;

namespace SCEAMS.Application.Interfaces;

public interface IAiProvider
{
    Task<AiProviderResult> GenerateAnswerAsync(
        AiPromptContext context,
        CancellationToken cancellationToken = default);
}
