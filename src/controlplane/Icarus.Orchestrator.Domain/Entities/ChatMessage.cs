namespace Icarus.Orchestrator.Domain.Entities;

public sealed class ChatMessage
{
    public Guid Id { get; private set; }
    public string Role { get; private set; } = string.Empty;
    public string Content { get; private set; } = string.Empty;
    public DateTime TimestampUtc { get; private set; }
    public IReadOnlyList<Citation> Citations { get; private set; } = [];

    private ChatMessage() { }

    public static ChatMessage FromUser(string content)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(content);

        return new ChatMessage
        {
            Id = Guid.NewGuid(),
            Role = "user",
            Content = content,
            TimestampUtc = DateTime.UtcNow
        };
    }

    public static ChatMessage FromAssistant(string content, IReadOnlyList<Citation>? citations = null)
    {
        return new ChatMessage
        {
            Id = Guid.NewGuid(),
            Role = "assistant",
            Content = content,
            TimestampUtc = DateTime.UtcNow,
            Citations = citations ?? []
        };
    }
}

public sealed record Citation(string DocumentId, string Snippet, float Score);
