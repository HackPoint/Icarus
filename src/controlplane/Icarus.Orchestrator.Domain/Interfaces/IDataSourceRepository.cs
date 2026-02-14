using Icarus.Orchestrator.Domain.Entities;

namespace Icarus.Orchestrator.Domain.Interfaces;

public interface IDataSourceRepository
{
    Task<DataSource?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<DataSource>> GetAllAsync(CancellationToken ct = default);
    Task AddAsync(DataSource source, CancellationToken ct = default);
    Task UpdateAsync(DataSource source, CancellationToken ct = default);
}
