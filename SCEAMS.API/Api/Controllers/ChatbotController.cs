using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SCEAMS.Application.DTOs.Chatbot;
using SCEAMS.Application.Interfaces;

namespace SCEAMS.Api.Controllers;

[Route("api/chatbot")]
[Authorize(Roles = "Student")]
[Produces("application/json")]
public sealed class ChatbotController : ApiControllerBase
{
    private readonly IEventFaqRetrievalService _retrievalService;
    private readonly IAiChatService _aiChatService;
    private readonly IChatHistoryService _chatHistoryService;

    public ChatbotController(
        IEventFaqRetrievalService retrievalService,
        IAiChatService aiChatService,
        IChatHistoryService chatHistoryService)
    {
        _retrievalService = retrievalService;
        _aiChatService = aiChatService;
        _chatHistoryService = chatHistoryService;
    }

    [HttpPost("retrieval")]
    [ProducesResponseType<EventFaqRetrievalResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> RetrieveEvents(
        [FromBody] EventFaqRetrievalRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await _retrievalService.RetrieveAsync(
            request,
            cancellationToken);

        return ToActionResult(result);
    }

    [HttpPost("ask")]
    [ProducesResponseType<AiChatResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Ask(
        [FromBody] AiChatRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await _aiChatService.AskAsync(
            request,
            User,
            cancellationToken);

        return ToActionResult(result);
    }

    [HttpGet("history")]
    [ProducesResponseType<ChatHistoryPageDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> History(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await _chatHistoryService.GetForCurrentStudentAsync(
            User,
            page,
            pageSize,
            cancellationToken);

        return ToActionResult(result);
    }
}
