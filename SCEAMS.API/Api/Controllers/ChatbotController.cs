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

    public ChatbotController(IEventFaqRetrievalService retrievalService)
    {
        _retrievalService = retrievalService;
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
}
