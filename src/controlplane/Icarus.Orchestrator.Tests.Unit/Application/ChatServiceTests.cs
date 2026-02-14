using FluentAssertions;
using Icarus.Orchestrator.Application.Contracts;
using Icarus.Orchestrator.Application.Services;
using Icarus.Orchestrator.Domain.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Icarus.Orchestrator.Tests.Unit.Application;

public class ChatServiceTests
{
    private readonly IVectorStore _vectorStore;
    private readonly IEmbeddingService _embeddingService;
    private readonly ILlmService _llmService;
    private readonly ChatService _sut;

    public ChatServiceTests()
    {
        _vectorStore = Substitute.For<IVectorStore>();
        _embeddingService = Substitute.For<IEmbeddingService>();
        _llmService = Substitute.For<ILlmService>();
        _sut = new ChatService(_vectorStore, _embeddingService, _llmService,
            NullLogger<ChatService>.Instance);
    }

    [Fact]
    public async Task QueryAsync_ShouldReturnResponseWithCitations()
    {
        var queryVector = new float[] { 0.1f, 0.2f, 0.3f };
        _embeddingService.EmbedAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(queryVector);

        var searchResults = new List<VectorSearchResult>
        {
            new("doc-1", 0.95f, new Dictionary<string, string> { ["content"] = "Test content" })
        };
        _vectorStore.SearchAsync(queryVector, 5, Arg.Any<CancellationToken>())
            .Returns(searchResults);

        _llmService.GenerateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("Generated answer based on context");

        var result = await _sut.QueryAsync(new ChatRequest("test query"));

        result.Should().NotBeNull();
        result.Answer.Should().Be("Generated answer based on context");
        result.Citations.Should().HaveCount(1);
        result.Citations[0].DocumentId.Should().Be("doc-1");
    }

    [Fact]
    public async Task StreamAsync_ShouldYieldAllEventTypes()
    {
        var queryVector = new float[] { 0.1f, 0.2f };
        _embeddingService.EmbedAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(queryVector);

        _vectorStore.SearchAsync(queryVector, Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<VectorSearchResult>
            {
                new("doc-1", 0.9f, new Dictionary<string, string> { ["content"] = "Context" })
            });

        _llmService.StreamAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(AsyncTokens("Hello", " world"));

        var events = new List<SseEvent>();
        await foreach (var evt in _sut.StreamAsync(new ChatRequest("test")))
        {
            events.Add(evt);
        }

        events.Should().Contain(e => e is ToolCallEvent);
        events.Should().Contain(e => e is ToolResultEvent);
        events.Should().Contain(e => e is CitationEvent);
        events.Should().Contain(e => e is TokenEvent);
        events.Should().Contain(e => e is FinalEvent);
        events.Should().Contain(e => e is MetricsEvent);
    }

    private static async IAsyncEnumerable<string> AsyncTokens(params string[] tokens)
    {
        foreach (var token in tokens)
        {
            await Task.Yield();
            yield return token;
        }
    }
}
