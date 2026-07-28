using SCEAMS.Application.Common;
using SCEAMS.Application.DTOs.Chatbot;

namespace SCEAMS.Application.Interfaces;

public interface IEventFaqRetrievalService
{
    Task<Result<EventFaqRetrievalResponseDto>> RetrieveAsync(
        EventFaqRetrievalRequestDto request,
        CancellationToken cancellationToken = default);
}
