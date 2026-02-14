using System.Text.Json.Serialization;

namespace Icarus.Orchestrator.Application.Contracts;

[JsonDerivedType(typeof(TokenEvent), "token")]
[JsonDerivedType(typeof(CitationEvent), "citation")]
[JsonDerivedType(typeof(ToolCallEvent), "tool_call")]
[JsonDerivedType(typeof(ToolResultEvent), "tool_result")]
[JsonDerivedType(typeof(FinalEvent), "final")]
[JsonDerivedType(typeof(ErrorEvent), "error")]
[JsonDerivedType(typeof(MetricsEvent), "metrics")]
public abstract record SseEvent(string Type);

public sealed record TokenEvent(string Token) : SseEvent("token");
public sealed record CitationEvent(string DocumentId, string Snippet, float Score) : SseEvent("citation");
public sealed record ToolCallEvent(string ToolName, Dictionary<string, object> Arguments) : SseEvent("tool_call");
public sealed record ToolResultEvent(string ToolName, string Result) : SseEvent("tool_result");
public sealed record FinalEvent(string FullText) : SseEvent("final");
public sealed record ErrorEvent(string Message, string Code) : SseEvent("error");
public sealed record MetricsEvent(long RetrievalMs, long GenerationMs, int TokenCount) : SseEvent("metrics");
