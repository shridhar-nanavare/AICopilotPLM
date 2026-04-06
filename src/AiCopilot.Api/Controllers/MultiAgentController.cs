using AiCopilot.Application.Abstractions;
using AiCopilot.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AiCopilot.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "EngineerAccess")]
public sealed class MultiAgentController : ControllerBase
{
    private readonly IMultiAgentOrchestrator _multiAgentOrchestrator;

    public MultiAgentController(IMultiAgentOrchestrator multiAgentOrchestrator)
    {
        _multiAgentOrchestrator = multiAgentOrchestrator;
    }

    [HttpPost]
    [ProducesResponseType(typeof(MultiAgentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<MultiAgentResponse>> Post(
        [FromBody] MultiAgentRequest request,
        CancellationToken cancellationToken)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Query))
        {
            return BadRequest(new { error = "Query is required." });
        }

        var response = await _multiAgentOrchestrator.ExecuteAsync(request, cancellationToken);
        return Ok(response);
    }
}
