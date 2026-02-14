using Icarus.Orchestrator.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Icarus.Retrieval;

public interface IRetrievalService
{
    Task<RetrievalResult> RetrieveAsync(string query, int topK = 5, CancellationToken ct = default);
}

public sealed record RetrievalResult(
    IReadOnlyList<RetrievedDocument> Documents,
    long ElapsedMs);

public sealed record RetrievedDocument(
    string DocumentId,
    string Content,
    float Score,
    Dictionary<string, string> Metadata);

public sealed class RetrievalService : IRetrievalService
{
    private readonly IEmbeddingService _embeddingService;
    private readonly IVectorStore _vectorStore;
    private readonly ILogger<RetrievalService> _logger;

    public RetrievalService(
        IEmbeddingService embeddingService,
        IVectorStore vectorStore,
        ILogger<RetrievalService> logger)
    {
        _embeddingService = embeddingService;
        _vectorStore = vectorStore;
        _logger = logger;
    }

    public async Task<RetrievalResult> RetrieveAsync(string query, int topK = 5, CancellationToken ct = default)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();

        _logger.LogInformation("Retrieving documents for query: {Query}", query);

        var queryVector = await _embeddingService.EmbedAsync(query, ct);
        var results = await _vectorStore.SearchAsync(queryVector, topK, ct);

        var documents = results.Select(r => new RetrievedDocument(
            r.DocumentId,
            r.Metadata.GetValueOrDefault("content", ""),
            r.Score,
            r.Metadata))
            .ToList();

        sw.Stop();
        _logger.LogInformation("Retrieved {Count} documents in {Elapsed}ms", documents.Count, sw.ElapsedMilliseconds);

        return new RetrievalResult(documents, sw.ElapsedMilliseconds);
    }
}
