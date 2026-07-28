using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using SCEAMS.Application.Common;
using SCEAMS.Application.DTOs.Chatbot;
using SCEAMS.Application.Interfaces;

namespace SCEAMS.Application.Services;

public sealed class AiChatService : IAiChatService
{
    private readonly IEventFaqRetrievalService _retrievalService;
    private readonly IAiProvider _aiProvider;
    private readonly IChatHistoryService _chatHistoryService;

    public AiChatService(
        IEventFaqRetrievalService retrievalService,
        IAiProvider aiProvider,
        IChatHistoryService chatHistoryService)
    {
        _retrievalService = retrievalService;
        _aiProvider = aiProvider;
        _chatHistoryService = chatHistoryService;
    }

    public async Task<Result<AiChatResponseDto>> AskAsync(
        AiChatRequestDto request,
        ClaimsPrincipal user,
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
            const string emptyAnswer =
                "Không tìm thấy Event Approved phù hợp với câu hỏi của bạn.";
            var emptyHistory = await _chatHistoryService.SaveAsync(
                question,
                emptyAnswer,
                [],
                user,
                cancellationToken);
            if (!emptyHistory.Success)
            {
                return Result<AiChatResponseDto>.Fail(
                    emptyHistory.Message,
                    emptyHistory.StatusCode);
            }

            return Result<AiChatResponseDto>.Ok(new AiChatResponseDto
            {
                Question = question,
                Answer = emptyAnswer,
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

        var history = await _chatHistoryService.SaveAsync(
            question,
            provider.Answer,
            relatedEvents.Select(eventItem => eventItem.Id).ToList(),
            user,
            cancellationToken);
        if (!history.Success)
        {
            return Result<AiChatResponseDto>.Fail(
                history.Message,
                history.StatusCode);
        }

        return Result<AiChatResponseDto>.Ok(new AiChatResponseDto
        {
            Question = question,
            Answer = provider.Answer.Trim(),
            RelatedEvents = relatedEvents
        });
    }
}
