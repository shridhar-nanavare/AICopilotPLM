using AiCopilot.Application.Abstractions;
using AiCopilot.Shared.Models;
using Microsoft.AspNetCore.Mvc;

namespace AiCopilot.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class FeedbackController : ControllerBase
{
    private readonly IFeedbackService _feedbackService;

    public FeedbackController(IFeedbackService feedbackService)
    {
        _feedbackService = feedbackService;
    }

    [HttpPost]
    [ProducesResponseType(typeof(FeedbackResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<FeedbackResponse>> Post(
        [FromBody] FeedbackRequest request,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return BadRequest(new { error = "Request is required." });
        }

        try
        {
            var response = await _feedbackService.SubmitAsync(request, cancellationToken);
            return Ok(response);
        }
        catch (ArgumentOutOfRangeException exception)
        {
            return BadRequest(new { error = exception.Message });
        }
        catch (InvalidOperationException exception)
        {
            return NotFound(new { error = exception.Message });
        }
    }
}
