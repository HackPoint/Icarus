using Icarus.Orchestrator.Application.Contracts;

namespace Icarus.Orchestrator.Application.Interfaces;

public interface ISourceService
{
    Task<RegisterSourceResponse> RegisterAsync(RegisterSourceRequest request, CancellationToken ct = default);
    Task BootstrapAsync(Guid sourceId, CancellationToken ct = default);
}
