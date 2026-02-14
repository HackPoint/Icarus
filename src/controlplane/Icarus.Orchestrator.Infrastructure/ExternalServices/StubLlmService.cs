using System.Runtime.CompilerServices;
using Icarus.Orchestrator.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Icarus.Orchestrator.Infrastructure.ExternalServices;

/// <summary>
/// Deterministic LLM stub. In production, calls Ollama HTTP API.
/// Returns a canned response that references the context.
/// </summary>
public sealed class StubLlmService : ILlmService
{
    private readonly ILogger<StubLlmService> _logger;

    public StubLlmService(ILogger<StubLlmService> logger)
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
            await Task.Delay(10, ct); // Simulate token generation latency
            yield return word;
        }
    }
}
