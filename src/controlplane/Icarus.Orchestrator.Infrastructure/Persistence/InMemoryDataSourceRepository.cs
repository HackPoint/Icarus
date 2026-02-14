using System.Collections.Concurrent;
using Icarus.Orchestrator.Domain.Entities;
using Icarus.Orchestrator.Domain.Interfaces;

namespace Icarus.Orchestrator.Infrastructure.Persistence;

public sealed class InMemoryDataSourceRepository : IDataSourceRepository
{
    private readonly ConcurrentDictionary<Guid, DataSource> _store = new();

    public Task<DataSource?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        _store.TryGetValue(id, out var source);
        return Task.FromResult(source);
    }

    public Task<IReadOnlyList<DataSource>> GetAllAsync(CancellationToken ct = default)
    {
        IReadOnlyList<DataSource> all = _store.Values.ToList();
        return Task.FromResult(all);
    }

    public Task AddAsync(DataSource source, CancellationToken ct = default)
    {
        _store.TryAdd(source.Id, source);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(DataSource source, CancellationToken ct = default)
    {
        _store[source.Id] = source;
        return Task.CompletedTask;
    }
}
