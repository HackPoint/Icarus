using System.Collections.Concurrent;
using Icarus.Indexer;
using Microsoft.Extensions.Logging;

namespace Icarus.Dataplane.Tests.Integration;

internal sealed class TestStubEmbeddingService : IIndexEmbeddingService
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

internal sealed class TestStubVectorStore : IIndexVectorStore
{
    private readonly ConcurrentDictionary<string, (float[] Vector, Dictionary<string, string> Metadata)> _store = new();

    public Task UpsertAsync(string documentId, float[] vector, Dictionary<string, string> metadata, CancellationToken ct = default)
    {
        _store[documentId] = (vector, metadata);
        return Task.CompletedTask;
    }
}
