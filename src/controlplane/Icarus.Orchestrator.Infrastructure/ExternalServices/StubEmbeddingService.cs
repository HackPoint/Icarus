using Icarus.Orchestrator.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Icarus.Orchestrator.Infrastructure.ExternalServices;

/// <summary>
/// Deterministic embedding stub. In production, calls the Rust embeddings-rs microservice.
/// Generates a 384-dimensional vector from a hash of the input text.
/// </summary>
public sealed class StubEmbeddingService : IEmbeddingService
{
    private const int Dimensions = 384;
    private readonly ILogger<StubEmbeddingService> _logger;

    public StubEmbeddingService(ILogger<StubEmbeddingService> logger)
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

        // Normalize
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
