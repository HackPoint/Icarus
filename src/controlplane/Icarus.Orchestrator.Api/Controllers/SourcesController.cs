using Icarus.Orchestrator.Application.Contracts;
using Icarus.Orchestrator.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Icarus.Orchestrator.Api.Controllers;

[ApiController]
[Route("sources")]
public sealed class SourcesController : ControllerBase
{
    private readonly ISourceService _sourceService;

    public SourcesController(ISourceService sourceService)
    {
        _sourceService = sourceService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(
        [FromBody] RegisterSourceRequest request,
        CancellationToken ct)
    {
        var result = await _sourceService.RegisterAsync(request, ct);
        return CreatedAtAction(nameof(Register), new { id = result.Id }, result);
    }

    [HttpPost("{id:guid}/bootstrap")]
    public async Task<IActionResult> Bootstrap(Guid id, CancellationToken ct)
    {
        await _sourceService.BootstrapAsync(id, ct);
        return Accepted(new { message = $"Bootstrap started for source {id}" });
    }
}
