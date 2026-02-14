using FluentAssertions;
using Icarus.Rag;
using Icarus.Retrieval;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Icarus.Rag.Tests.Integration;

public class RagIntegrationTests
{
    [Fact]
    public async Task FullRagPipeline_WithStubs_ShouldProduceDeterministicOutput()
    {
        var embeddingService = new TestStubEmbeddingService(NullLogger<TestStubEmbeddingService>.Instance);
        var vectorStore = new TestStubVectorStore();
        var llmService = new TestStubLlmService(NullLogger<TestStubLlmService>.Instance);

        var retrieval = new RetrievalService(embeddingService, vectorStore,
            NullLogger<RetrievalService>.Instance);

        var pipeline = new RagPipeline(retrieval, llmService,
            NullLogger<RagPipeline>.Instance);

        var result = await pipeline.ExecuteAsync("What is Icarus?");

        result.Should().NotBeNull();
        result.Answer.Should().NotBeNullOrEmpty();
        result.Sources.Should().NotBeEmpty(); // StubVectorStore returns seed results
        result.RetrievalMs.Should().BeGreaterOrEqualTo(0);
        result.GenerationMs.Should().BeGreaterOrEqualTo(0);
    }
}
