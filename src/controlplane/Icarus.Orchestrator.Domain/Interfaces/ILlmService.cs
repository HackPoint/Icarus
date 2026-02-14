namespace Icarus.Orchestrator.Domain.Interfaces;

public interface ILlmService
{
    Task<string> GenerateAsync(string systemPrompt, string userPrompt, CancellationToken ct = default);
    IAsyncEnumerable<string> StreamAsync(string systemPrompt, string userPrompt, CancellationToken ct = default);
}
