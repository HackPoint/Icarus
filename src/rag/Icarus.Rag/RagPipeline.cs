using System.Diagnostics;
using Icarus.Orchestrator.Domain.Interfaces;
using Icarus.Retrieval;
using Microsoft.Extensions.Logging;

namespace Icarus.Rag;

public interface IRagPipeline
{
    Task<RagResponse> ExecuteAsync(string query, int topK = 5, CancellationToken ct = default);
}

public sealed record RagResponse(
    string Answer,
    IReadOnlyList<RetrievedDocument> Sources,
    long RetrievalMs,
    long GenerationMs);

public sealed class RagPipeline : IRagPipeline
{
    private readonly IRetrievalService _retrieval;
    private readonly ILlmService _llmService;
    private readonly ILogger<RagPipeline> _logger;

    public RagPipeline(
        IRetrievalService retrieval,
        ILlmService llmService,
        ILogger<RagPipeline> logger)
    {
        _retrieval = retrieval;
        _llmService = llmService;
        _logger = logger;
    }

    public async Task<RagResponse> ExecuteAsync(string query, int topK = 5, CancellationToken ct = default)
    {
        _logger.LogInformation("Executing RAG pipeline for: {Query}", query);

        // Step 1: Retrieve
        var retrievalResult = await _retrieval.RetrieveAsync(query, topK, ct);

        // Step 2: Build prompt context
        var context = string.Join("\n---\n",
            retrievalResult.Documents.Select(d => d.Content));

        var systemPrompt = $"""
            You are Icarus, a helpful AI assistant.
            Answer the user's question based on the following retrieved documents.
            If the documents don't contain relevant information, say so.

            Documents:
            {context}
            """;

        // Step 3: Generate
        var sw = Stopwatch.StartNew();
        var answer = await _llmService.GenerateAsync(systemPrompt, query, ct);
        var generationMs = sw.ElapsedMilliseconds;

        return new RagResponse(
            answer,
            retrievalResult.Documents,
            retrievalResult.ElapsedMs,
            generationMs);
    }
}
