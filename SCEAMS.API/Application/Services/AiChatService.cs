using Microsoft.AspNetCore.Http;
using SCEAMS.Application.Common;
using SCEAMS.Application.DTOs.Chatbot;
using SCEAMS.Application.Interfaces;

namespace SCEAMS.Application.Services;

public sealed class AiChatService : IAiChatService
{
    private readonly IEventFaqRetrievalService _retrievalService;
    private readonly IAiProvider _aiProvider;

    public AiChatService(
        IEventFaqRetrievalService retrievalService,
        IAiProvider aiProvider)
    {
        _retrievalService = retrievalService;
        _aiProvider = aiProvider;
    }

    public async Task<Result<AiChatResponseDto>> AskAsync(
        AiChatRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var question = request.Question.Trim();
        var retrieval = await _retrievalService.RetrieveAsync(
            new EventFaqRetrievalRequestDto { Question = question },
            cancellationToken);
        if (!retrieval.Success)
        {
            return Result<AiChatResponseDto>.Fail(
                retrieval.Message,
                retrieval.StatusCode,
                retrieval.ErrorData!);
        }

        var relatedEvents = retrieval.Data?.RelatedEvents ?? [];
        if (relatedEvents.Count == 0)
        {
            return Result<AiChatResponseDto>.Ok(new AiChatResponseDto
            {
                Question = question,
                Answer = "Không tìm thấy Event Approved phù hợp với câu hỏi của bạn.",
                RelatedEvents = []
            });
        }

        var context = new AiPromptContext(
            question,
            relatedEvents.Select(eventItem => new AiEventContextDto(
                eventItem.Id,
                eventItem.Title,
                eventItem.ClubName,
                eventItem.VenueName,
                eventItem.StartTime,
                eventItem.EndTime,
                eventItem.Capacity,
                eventItem.RegisteredCount,
                eventItem.SlotsRemaining)).ToList());
        var provider = await _aiProvider.GenerateAnswerAsync(
            context,
            cancellationToken);
        if (!provider.IsSuccess || string.IsNullOrWhiteSpace(provider.Answer))
        {
            return Result<AiChatResponseDto>.Fail(
                provider.ErrorMessage ?? "AI provider hiện không khả dụng.",
                StatusCodes.Status503ServiceUnavailable);
        }

        return Result<AiChatResponseDto>.Ok(new AiChatResponseDto
        {
            Question = question,
            Answer = provider.Answer.Trim(),
            RelatedEvents = relatedEvents
        });
    }
}
