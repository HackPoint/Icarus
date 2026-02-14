using System.Diagnostics;
using System.Runtime.CompilerServices;
using Icarus.Orchestrator.Application.Contracts;
using Icarus.Orchestrator.Application.Interfaces;
using Icarus.Orchestrator.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Icarus.Orchestrator.Application.Services;

public sealed class ChatService : IChatService
{
    private readonly IVectorStore _vectorStore;
    private readonly IEmbeddingService _embeddingService;
    private readonly ILlmService _llmService;
    private readonly ILogger<ChatService> _logger;

    public ChatService(
        IVectorStore vectorStore,
        IEmbeddingService embeddingService,
        ILlmService llmService,
        ILogger<ChatService> logger)
    {
        _vectorStore = vectorStore;
        _embeddingService = embeddingService;
        _llmService = llmService;
        _logger = logger;
    }

    public async Task<ChatResponse> QueryAsync(ChatRequest request, CancellationToken ct = default)
    {
        _logger.LogInformation("Processing chat query: {Query}", request.Query);

        var sw = Stopwatch.StartNew();

        // 1. Embed the query
        var queryVector = await _embeddingService.EmbedAsync(request.Query, ct);

        // 2. Retrieve relevant documents
        var results = await _vectorStore.SearchAsync(queryVector, request.TopK, ct);
        var retrievalMs = sw.ElapsedMilliseconds;

        // 3. Build context from retrieved documents
        var context = string.Join("\n\n", results.Select(r =>
            r.Metadata.TryGetValue("content", out var c) ? c : $"[Document: {r.DocumentId}]"));

        var systemPrompt = $"""
            You are Icarus, an AI assistant for answering questions based on retrieved documents.
            Use the following context to answer the user's question. If the context doesn't contain
            relevant information, say so honestly.

            Context:
            {context}
            """;

        // 4. Generate answer
        sw.Restart();
        var answer = await _llmService.GenerateAsync(systemPrompt, request.Query, ct);
        var generationMs = sw.ElapsedMilliseconds;

        var citations = results.Select(r =>
            new CitationDto(r.DocumentId, r.Metadata.GetValueOrDefault("content", ""), r.Score))
            .ToList();

        return new ChatResponse(
            answer,
            citations,
            new ChatMetrics(retrievalMs, generationMs, answer.Length / 4));
    }

    public async IAsyncEnumerable<SseEvent> StreamAsync(
        ChatRequest request,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        _logger.LogInformation("Starting streaming chat for: {Query}", request.Query);

        var sw = Stopwatch.StartNew();

        // Tool call event
        yield return new ToolCallEvent("embed_query", new Dictionary<string, object>
        {
            ["query"] = request.Query
        });

        var queryVector = await _embeddingService.EmbedAsync(request.Query, ct);

        yield return new ToolResultEvent("embed_query", "Embedding generated");

        // Retrieval
        yield return new ToolCallEvent("vector_search", new Dictionary<string, object>
        {
            ["topK"] = request.TopK
        });

        var results = await _vectorStore.SearchAsync(queryVector, request.TopK, ct);
        var retrievalMs = sw.ElapsedMilliseconds;

        yield return new ToolResultEvent("vector_search", $"Found {results.Count} results");

        // Citations
        foreach (var result in results)
        {
            yield return new CitationEvent(
                result.DocumentId,
                result.Metadata.GetValueOrDefault("content", ""),
                result.Score);
        }

        // Build context
        var context = string.Join("\n\n", results.Select(r =>
            r.Metadata.TryGetValue("content", out var c) ? c : $"[Document: {r.DocumentId}]"));

        var systemPrompt = $"""
            You are Icarus, an AI assistant. Use the following context to answer questions.
            Context:
            {context}
            """;

        // Stream tokens
        sw.Restart();
        var fullText = new System.Text.StringBuilder();

        await foreach (var token in _llmService.StreamAsync(systemPrompt, request.Query, ct))
        {
            fullText.Append(token);
            yield return new TokenEvent(token);
        }

        var generationMs = sw.ElapsedMilliseconds;

        yield return new FinalEvent(fullText.ToString());
        yield return new MetricsEvent(retrievalMs, generationMs, fullText.Length / 4);
    }
}
