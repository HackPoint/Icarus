namespace Icarus.Orchestrator.Mcp.Protocol;

public sealed record McpToolDefinition(
    string Name,
    string Description,
    Dictionary<string, McpParameterSchema> InputSchema);

public sealed record McpParameterSchema(
    string Type,
    string Description,
    bool Required = false);

public sealed record McpToolCallRequest(
    string Tool,
    Dictionary<string, object> Arguments);

public sealed record McpToolCallResponse(
    string Tool,
    bool Success,
    object Result,
    string? Error = null);

public sealed record McpListToolsResponse(
    IReadOnlyList<McpToolDefinition> Tools);
