using AiCopilot.Application.Abstractions;
using AiCopilot.Shared.Models;
using Microsoft.AspNetCore.Mvc;

namespace AiCopilot.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class PromptsController : ControllerBase
{
    private readonly IPromptProcessor _promptProcessor;

    public PromptsController(IPromptProcessor promptProcessor)
    {
        _promptProcessor = promptProcessor;
    }

    [HttpPost]
    [ProducesResponseType(typeof(PromptResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<PromptResponse>> ProcessPrompt(
        [FromBody] PromptRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _promptProcessor.ProcessAsync(request, cancellationToken);
        return Ok(response);
    }
}
