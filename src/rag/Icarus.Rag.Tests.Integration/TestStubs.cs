using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Icarus.Orchestrator.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Icarus.Rag.Tests.Integration;

internal sealed class TestStubEmbeddingService : IEmbeddingService
{
    private const int Dimensions = 384;
    private readonly ILogger<TestStubEmbeddingService> _logger;

    public TestStubEmbeddingService(ILogger<TestStubEmbeddingService> logger)
    {
        _logger = logger;
    }

    public Task<float[]> EmbedAsync(string text, CancellationToken ct = default)
    {
        _logger.LogDebug("Generating stub embedding for text of length {Length}", text.Length);

        var vector = new float[Dimensions];
        var hash = text.GetHashCode();
        var rng = new Random(hash);

        for (int i = 0; i < Dimensions; i++)
        {
            vector[i] = (float)(rng.NextDouble() * 2 - 1);
        }

        var magnitude = MathF.Sqrt(vector.Sum(v => v * v));
        if (magnitude > 0)
        {
            for (int i = 0; i < Dimensions; i++)
            {
                vector[i] /= magnitude;
            }
        }

        return Task.FromResult(vector);
    }
}

internal sealed class TestStubVectorStore : IVectorStore
{
    private readonly ConcurrentDictionary<string, (float[] Vector, Dictionary<string, string> Metadata)> _store = new();

    public Task<IReadOnlyList<VectorSearchResult>> SearchAsync(
        float[] queryVector, int topK = 5, CancellationToken ct = default)
    {
        if (_store.IsEmpty)
        {
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

internal sealed class TestStubLlmService : ILlmService
{
    private readonly ILogger<TestStubLlmService> _logger;

    public TestStubLlmService(ILogger<TestStubLlmService> logger)
    {
        _logger = logger;
    }

    public Task<string> GenerateAsync(string systemPrompt, string userPrompt, CancellationToken ct = default)
    {
        _logger.LogDebug("Generating stub LLM response for prompt: {Prompt}", userPrompt);

        var response = $"Based on the available documents, here is what I found regarding your query " +
                       $"\"{userPrompt}\": The Icarus platform processes your request through retrieval-augmented " +
                       $"generation, combining document search with language model inference to provide " +
                       $"accurate, cited responses. This is a deterministic stub response for development.";

        return Task.FromResult(response);
    }

    public async IAsyncEnumerable<string> StreamAsync(
        string systemPrompt,
        string userPrompt,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        _logger.LogDebug("Streaming stub LLM response for: {Prompt}", userPrompt);

        var words = new[]
        {
            "Based", " on", " the", " available", " documents,",
            " here", " is", " what", " I", " found",
            " regarding", " your", " query.", " The", " Icarus",
            " platform", " processes", " your", " request", " through",
            " retrieval-augmented", " generation.", " This", " is", " a",
            " deterministic", " stub", " response", " for", " development."
        };

        foreach (var word in words)
        {
            ct.ThrowIfCancellationRequested();
            await Task.Delay(10, ct);
            yield return word;
        }
    }
}
