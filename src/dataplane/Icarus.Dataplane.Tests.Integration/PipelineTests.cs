using FluentAssertions;
using Icarus.Chunker;
using Icarus.Indexer;
using Icarus.Normalizer;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Icarus.Dataplane.Tests.Integration;

public class PipelineTests
{
    [Fact]
    public async Task FullPipeline_NormalizeChunkIndex_ShouldComplete()
    {
        // Arrange
        var normalizer = new DocumentNormalizer(NullLogger<DocumentNormalizer>.Instance);
        var chunker = new TextChunker(NullLogger<TextChunker>.Instance);
        var embeddingService = new TestStubEmbeddingService(NullLogger<TestStubEmbeddingService>.Instance);
        var vectorStore = new TestStubVectorStore();
        var indexer = new VectorIndexer(embeddingService, vectorStore, NullLogger<VectorIndexer>.Instance);

        var rawContent = "This is a sample document about Icarus platform. " +
                         "It provides RAG capabilities for enterprise document intelligence. " +
                         "The system supports multiple data sources and embedding models.";

        // Act
        var normalized = normalizer.Normalize(rawContent, "source-1", "doc-1");
        var chunks = chunker.Chunk(normalized);
        await indexer.IndexChunksAsync(chunks);

        // Assert
        normalized.Content.Should().NotBeNullOrEmpty();
        chunks.Should().NotBeEmpty();
        chunks[0].DocumentId.Should().Be("doc-1");
    }
}
