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

    public ChatbotController(
        IEventFaqRetrievalService retrievalService,
        IAiChatService aiChatService)
    {
        _retrievalService = retrievalService;
        _aiChatService = aiChatService;
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
            cancellationToken);

        return ToActionResult(result);
    }
}
