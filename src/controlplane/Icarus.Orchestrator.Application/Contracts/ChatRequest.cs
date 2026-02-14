namespace Icarus.Orchestrator.Application.Contracts;

public sealed record ChatRequest(string Query, int TopK = 5);

public sealed record ChatResponse(
    string Answer,
    IReadOnlyList<CitationDto> Citations,
    ChatMetrics Metrics);

public sealed record CitationDto(string DocumentId, string Snippet, float Score);

public sealed record ChatMetrics(long RetrievalMs, long GenerationMs, int TokenCount);
