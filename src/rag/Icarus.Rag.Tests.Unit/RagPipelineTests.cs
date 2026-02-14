using FluentAssertions;
using Icarus.Orchestrator.Domain.Interfaces;
using Icarus.Rag;
using Icarus.Retrieval;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Icarus.Rag.Tests.Unit;

public class RagPipelineTests
{
    private readonly IRetrievalService _retrieval;
    private readonly ILlmService _llmService;
    private readonly RagPipeline _sut;

    public RagPipelineTests()
    {
        _retrieval = Substitute.For<IRetrievalService>();
        _llmService = Substitute.For<ILlmService>();
        _sut = new RagPipeline(_retrieval, _llmService, NullLogger<RagPipeline>.Instance);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnAnswerWithSources()
    {
        _retrieval.RetrieveAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new RetrievalResult(
                new List<RetrievedDocument>
                {
                    new("doc-1", "Content about Icarus", 0.95f, new Dictionary<string, string>())
                },
                ElapsedMs: 10));

        _llmService.GenerateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("Icarus is a RAG platform.");

        var result = await _sut.ExecuteAsync("What is Icarus?");

        result.Should().NotBeNull();
        result.Answer.Should().Be("Icarus is a RAG platform.");
        result.Sources.Should().HaveCount(1);
        result.RetrievalMs.Should().Be(10);
    }

    [Fact]
    public async Task ExecuteAsync_WithNoResults_ShouldStillReturnAnswer()
    {
        _retrieval.RetrieveAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new RetrievalResult([], ElapsedMs: 5));

        _llmService.GenerateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("I don't have relevant information.");

        var result = await _sut.ExecuteAsync("Unknown topic");

        result.Answer.Should().Contain("don't have");
        result.Sources.Should().BeEmpty();
    }
}
