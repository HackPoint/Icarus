using Icarus.Normalizer;
using Microsoft.Extensions.Logging;

namespace Icarus.Chunker;

public interface ITextChunker
{
    IReadOnlyList<TextChunk> Chunk(NormalizedDocument document, int chunkSize = 512, int overlap = 64);
}

public sealed record TextChunk(
    string ChunkId,
    string DocumentId,
    string Content,
    int StartOffset,
    int EndOffset,
    Dictionary<string, string> Metadata);

public sealed class TextChunker : ITextChunker
{
    private readonly ILogger<TextChunker> _logger;

    public TextChunker(ILogger<TextChunker> logger)
    {
        _logger = logger;
    }

    public IReadOnlyList<TextChunk> Chunk(NormalizedDocument document, int chunkSize = 512, int overlap = 64)
    {
        _logger.LogDebug("Chunking document {Id}, length={Length}, chunkSize={ChunkSize}",
            document.Id, document.Content.Length, chunkSize);

        var chunks = new List<TextChunk>();
        var content = document.Content;

        if (string.IsNullOrWhiteSpace(content))
        {
            return chunks;
        }

        int offset = 0;
        int chunkIndex = 0;

        while (offset < content.Length)
        {
            var end = Math.Min(offset + chunkSize, content.Length);
            var chunkContent = content[offset..end];

            chunks.Add(new TextChunk(
                ChunkId: $"{document.Id}-chunk-{chunkIndex}",
                DocumentId: document.Id,
                Content: chunkContent,
                StartOffset: offset,
                EndOffset: end,
                Metadata: new Dictionary<string, string>(document.Metadata)
                {
                    ["chunkIndex"] = chunkIndex.ToString(),
                    ["content"] = chunkContent
                }));

            offset += chunkSize - overlap;
            chunkIndex++;
        }

        _logger.LogDebug("Document {Id} split into {Count} chunks", document.Id, chunks.Count);
        return chunks;
    }
}
