using Icarus.Chunker;
using Microsoft.Extensions.Logging;

namespace Icarus.Indexer;

public interface IVectorIndexer
{
    Task IndexChunksAsync(IReadOnlyList<TextChunk> chunks, CancellationToken ct = default);
}

public sealed class VectorIndexer : IVectorIndexer
{
    private readonly IIndexEmbeddingService _embeddingService;
    private readonly IIndexVectorStore _vectorStore;
    private readonly ILogger<VectorIndexer> _logger;

    public VectorIndexer(
        IIndexEmbeddingService embeddingService,
        IIndexVectorStore vectorStore,
        ILogger<VectorIndexer> logger)
    {
        _embeddingService = embeddingService;
        _vectorStore = vectorStore;
        _logger = logger;
    }

    public async Task IndexChunksAsync(IReadOnlyList<TextChunk> chunks, CancellationToken ct = default)
    {
        _logger.LogInformation("Indexing {Count} chunks", chunks.Count);

        foreach (var chunk in chunks)
        {
            ct.ThrowIfCancellationRequested();

            var vector = await _embeddingService.EmbedAsync(chunk.Content, ct);
            await _vectorStore.UpsertAsync(chunk.ChunkId, vector, chunk.Metadata, ct);

            _logger.LogDebug("Indexed chunk {ChunkId}", chunk.ChunkId);
        }

        _logger.LogInformation("Finished indexing {Count} chunks", chunks.Count);
    }
}
