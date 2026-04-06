using AiCopilot.Infrastructure.Services;
using AiCopilot.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AiCopilot.Api.Controllers;

[ApiController]
[Route("api/mock/plm")]
[AllowAnonymous]
public sealed class PlmMockController : ControllerBase
{
    private readonly IPlmMockApiService _plmMockApiService;

    public PlmMockController(IPlmMockApiService plmMockApiService)
    {
        _plmMockApiService = plmMockApiService;
    }

    [HttpPost("create-part")]
    [ProducesResponseType(typeof(CreatePartResult), StatusCodes.Status200OK)]
    public async Task<ActionResult<CreatePartResult>> CreatePart(
        [FromBody] CreatePartRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _plmMockApiService.CreatePartAsync(request, cancellationToken);
        return Ok(response);
    }

    [HttpPost("find-duplicate")]
    [ProducesResponseType(typeof(FindDuplicateResult), StatusCodes.Status200OK)]
    public async Task<ActionResult<FindDuplicateResult>> FindDuplicate(
        [FromBody] FindDuplicateRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _plmMockApiService.FindDuplicateAsync(request, cancellationToken);
        return Ok(response);
    }

    [HttpPost("analyze-bom")]
    [ProducesResponseType(typeof(BomAnalysisResult), StatusCodes.Status200OK)]
    public async Task<ActionResult<BomAnalysisResult>> AnalyzeBom(
        [FromBody] AnalyzeBomRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _plmMockApiService.AnalyzeBomAsync(request, cancellationToken);
        return Ok(response);
    }
}
