namespace Icarus.Orchestrator.Domain.Interfaces;

public interface IVectorStore
{
    Task<IReadOnlyList<VectorSearchResult>> SearchAsync(
        float[] queryVector,
        int topK = 5,
        CancellationToken ct = default);

    Task UpsertAsync(string documentId, float[] vector, Dictionary<string, string> metadata, CancellationToken ct = default);
}

public sealed record VectorSearchResult(string DocumentId, float Score, Dictionary<string, string> Metadata);
