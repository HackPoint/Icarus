using Icarus.Orchestrator.Application.Contracts;

namespace Icarus.Orchestrator.Application.Interfaces;

public interface IChatService
{
    Task<ChatResponse> QueryAsync(ChatRequest request, CancellationToken ct = default);
    IAsyncEnumerable<SseEvent> StreamAsync(ChatRequest request, CancellationToken ct = default);
}
