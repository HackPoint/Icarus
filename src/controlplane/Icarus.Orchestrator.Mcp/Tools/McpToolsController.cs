using System.Text.Json;
using Icarus.Orchestrator.Application.Contracts;
using Icarus.Orchestrator.Application.Interfaces;
using Icarus.Orchestrator.Domain.Entities;
using Icarus.Orchestrator.Mcp.Protocol;
using Microsoft.AspNetCore.Mvc;

namespace Icarus.Orchestrator.Mcp.Tools;

[ApiController]
[Route("mcp")]
public sealed class McpToolsController : ControllerBase
{
    private readonly ISourceService _sourceService;
    private readonly IChatService _chatService;
    private readonly ILogger<McpToolsController> _logger;

    public McpToolsController(
        ISourceService sourceService,
        IChatService chatService,
        ILogger<McpToolsController> logger)
    {
        _sourceService = sourceService;
        _chatService = chatService;
        _logger = logger;
    }

    [HttpGet("tools")]
    public IActionResult ListTools()
    {
        var tools = new McpListToolsResponse(
        [
            new McpToolDefinition("register_source", "Register a new data source for indexing", new()
            {
                ["name"] = new("string", "Source name", true),
                ["connectionString"] = new("string", "Connection string to the data source", true),
                ["sourceType"] = new("string", "Type: CouchDb, Postgres, FileSystem, S3", true)
            }),
            new McpToolDefinition("bootstrap_source", "Start ingestion and indexing for a registered source", new()
            {
                ["sourceId"] = new("string", "GUID of the registered source", true)
            }),
            new McpToolDefinition("search", "Search indexed documents using semantic query", new()
            {
                ["query"] = new("string", "Search query text", true),
                ["topK"] = new("integer", "Number of results to return", false)
            }),
            new McpToolDefinition("chat_stream", "Start a streaming chat session", new()
            {
                ["query"] = new("string", "User question", true)
            }),
            new McpToolDefinition("generate_report", "Generate an analytics report", new()
            {
                ["reportType"] = new("string", "Type of report: usage, sources, queries", true)
            })
        ]);

        return Ok(tools);
    }

    [HttpPost("tools/call")]
    public async Task<IActionResult> CallTool(
        [FromBody] McpToolCallRequest request,
        CancellationToken ct)
    {
        _logger.LogInformation("MCP tool call: {Tool}", request.Tool);

        try
        {
            object result = request.Tool switch
            {
                "register_source" => await HandleRegisterSource(request.Arguments, ct),
                "bootstrap_source" => await HandleBootstrapSource(request.Arguments, ct),
                "search" => await HandleSearch(request.Arguments, ct),
                "chat_stream" => await HandleChat(request.Arguments, ct),
                "generate_report" => HandleGenerateReport(request.Arguments),
                _ => throw new ArgumentException($"Unknown tool: {request.Tool}")
            };

            return Ok(new McpToolCallResponse(request.Tool, true, result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MCP tool call failed: {Tool}", request.Tool);
            return Ok(new McpToolCallResponse(request.Tool, false, new { }, ex.Message));
        }
    }

    private async Task<object> HandleRegisterSource(Dictionary<string, object> args, CancellationToken ct)
    {
        var name = args["name"].ToString()!;
        var connStr = args["connectionString"].ToString()!;
        var sourceType = Enum.Parse<SourceType>(args["sourceType"].ToString()!);

        var req = new RegisterSourceRequest(name, connStr, sourceType);
        return await _sourceService.RegisterAsync(req, ct);
    }

    private async Task<object> HandleBootstrapSource(Dictionary<string, object> args, CancellationToken ct)
    {
        var sourceId = Guid.Parse(args["sourceId"].ToString()!);
        await _sourceService.BootstrapAsync(sourceId, ct);
        return new { status = "bootstrapped" };
    }

    private async Task<object> HandleSearch(Dictionary<string, object> args, CancellationToken ct)
    {
        var query = args["query"].ToString()!;
        var topK = args.TryGetValue("topK", out var tk) ? Convert.ToInt32(tk) : 5;

        var chatReq = new ChatRequest(query, topK);
        var response = await _chatService.QueryAsync(chatReq, ct);
        return response.Citations;
    }

    private async Task<object> HandleChat(Dictionary<string, object> args, CancellationToken ct)
    {
        var query = args["query"].ToString()!;
        var chatReq = new ChatRequest(query);
        var response = await _chatService.QueryAsync(chatReq, ct);
        return response;
    }

    private static object HandleGenerateReport(Dictionary<string, object> args)
    {
        var reportType = args["reportType"].ToString()!;
        return new
        {
            reportType,
            generatedAt = DateTime.UtcNow,
            data = new
            {
                totalSources = 3,
                totalDocuments = 150,
                totalQueries = 42,
                avgResponseTimeMs = 230
            }
        };
    }
}
