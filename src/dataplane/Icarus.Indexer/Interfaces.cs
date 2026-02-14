namespace Icarus.Indexer;

public interface IIndexEmbeddingService
{
    Task<float[]> EmbedAsync(string text, CancellationToken ct = default);
}

public interface IIndexVectorStore
{
    Task UpsertAsync(string documentId, float[] vector, Dictionary<string, string> metadata, CancellationToken ct = default);
}
