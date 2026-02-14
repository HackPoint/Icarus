using Microsoft.Extensions.Logging;

namespace Icarus.Normalizer;

public interface IDocumentNormalizer
{
    NormalizedDocument Normalize(string rawContent, string sourceId, string documentId);
}

public sealed record NormalizedDocument(
    string Id,
    string SourceId,
    string Content,
    Dictionary<string, string> Metadata,
    DateTime NormalizedAtUtc);

public sealed class DocumentNormalizer : IDocumentNormalizer
{
    private readonly ILogger<DocumentNormalizer> _logger;

    public DocumentNormalizer(ILogger<DocumentNormalizer> logger)
    {
        _logger = logger;
    }

    public NormalizedDocument Normalize(string rawContent, string sourceId, string documentId)
    {
        _logger.LogDebug("Normalizing document {DocumentId} from source {SourceId}", documentId, sourceId);

        // Strip HTML tags, normalize whitespace
        var content = rawContent
            .Replace("\r\n", "\n")
            .Replace("\r", "\n")
            .Trim();

        // Remove HTML tags (basic)
        content = System.Text.RegularExpressions.Regex.Replace(content, "<[^>]+>", " ");
        content = System.Text.RegularExpressions.Regex.Replace(content, @"\s+", " ").Trim();

        return new NormalizedDocument(
            Id: documentId,
            SourceId: sourceId,
            Content: content,
            Metadata: new Dictionary<string, string>
            {
                ["source"] = sourceId,
                ["originalLength"] = rawContent.Length.ToString(),
                ["normalizedLength"] = content.Length.ToString()
            },
            NormalizedAtUtc: DateTime.UtcNow);
    }
}
