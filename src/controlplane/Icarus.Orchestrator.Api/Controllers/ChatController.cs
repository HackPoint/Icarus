using System.Text.Json;
using Icarus.Orchestrator.Application.Contracts;
using Icarus.Orchestrator.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Icarus.Orchestrator.Api.Controllers;

[ApiController]
[Route("chat")]
public sealed class ChatController : ControllerBase
{
    private readonly IChatService _chatService;
    private readonly ILogger<ChatController> _logger;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public ChatController(IChatService chatService, ILogger<ChatController> logger)
    {
        _chatService = chatService;
        _logger = logger;
    }

    [HttpPost("query")]
    public async Task<IActionResult> Query(
        [FromBody] ChatRequest request,
        CancellationToken ct)
    {
        var result = await _chatService.QueryAsync(request, ct);
        return Ok(result);
    }

    [HttpGet("stream")]
    public async Task Stream(
        [FromQuery] string query,
        [FromQuery] int topK = 5,
        CancellationToken ct = default)
    {
        Response.ContentType = "text/event-stream";
        Response.Headers.CacheControl = "no-cache";
        Response.Headers.Connection = "keep-alive";

        var request = new ChatRequest(query, topK);

        try
        {
            await foreach (var sseEvent in _chatService.StreamAsync(request, ct))
            {
                var eventType = sseEvent.Type;
                var data = JsonSerializer.Serialize(sseEvent, sseEvent.GetType(), JsonOptions);

                await Response.WriteAsync($"event: {eventType}\n", ct);
                await Response.WriteAsync($"data: {data}\n\n", ct);
                await Response.Body.FlushAsync(ct);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("SSE stream cancelled by client");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during SSE streaming");

            var error = new ErrorEvent("Internal server error", "STREAM_ERROR");
            var errorData = JsonSerializer.Serialize(error, JsonOptions);

            await Response.WriteAsync($"event: error\n", ct);
            await Response.WriteAsync($"data: {errorData}\n\n", ct);
            await Response.Body.FlushAsync(ct);
        }
    }
}
