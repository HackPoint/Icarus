using System.Collections.Concurrent;
using Icarus.Orchestrator.Domain.Interfaces;

namespace Icarus.Orchestrator.Infrastructure.ExternalServices;

/// <summary>
/// In-memory vector store stub for development. Returns deterministic results.
/// In production, this would wrap the Qdrant gRPC/HTTP client.
/// </summary>
public sealed class StubVectorStore : IVectorStore
{
    private readonly ConcurrentDictionary<string, (float[] Vector, Dictionary<string, string> Metadata)> _store = new();

    public Task<IReadOnlyList<VectorSearchResult>> SearchAsync(
        float[] queryVector, int topK = 5, CancellationToken ct = default)
    {
        if (_store.IsEmpty)
        {
            // Return deterministic seed results when store is empty
            IReadOnlyList<VectorSearchResult> seedResults =
            [
                new VectorSearchResult("doc-001", 0.95f, new Dictionary<string, string>
                {
                    ["content"] = "Icarus is an on-prem RAG platform for enterprise document intelligence.",
                    ["source"] = "seed"
                }),
                new VectorSearchResult("doc-002", 0.87f, new Dictionary<string, string>
                {
                    ["content"] = "The platform supports multiple data sources including CouchDB and PostgreSQL.",
                    ["source"] = "seed"
                }),
                new VectorSearchResult("doc-003", 0.82f, new Dictionary<string, string>
                {
                    ["content"] = "Embeddings are generated using a Rust microservice for high performance.",
                    ["source"] = "seed"
                })
            ];
            return Task.FromResult(seedResults);
        }

        // Simple cosine-similarity-like scoring (dot product of normalized vectors)
        var results = _store
            .Select(kvp => new VectorSearchResult(
                kvp.Key,
                CosineSimilarity(queryVector, kvp.Value.Vector),
                kvp.Value.Metadata))
            .OrderByDescending(r => r.Score)
            .Take(topK)
            .ToList();

        return Task.FromResult<IReadOnlyList<VectorSearchResult>>(results);
    }

    public Task UpsertAsync(string documentId, float[] vector, Dictionary<string, string> metadata, CancellationToken ct = default)
    {
        _store[documentId] = (vector, metadata);
        return Task.CompletedTask;
    }

    private static float CosineSimilarity(float[] a, float[] b)
    {
        if (a.Length != b.Length) return 0f;

        float dot = 0, magA = 0, magB = 0;
        for (int i = 0; i < a.Length; i++)
        {
            dot += a[i] * b[i];
            magA += a[i] * a[i];
            magB += b[i] * b[i];
        }

        var denom = MathF.Sqrt(magA) * MathF.Sqrt(magB);
        return denom == 0 ? 0f : dot / denom;
    }
}
