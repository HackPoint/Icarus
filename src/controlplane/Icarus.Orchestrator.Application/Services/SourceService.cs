using Icarus.Orchestrator.Application.Contracts;
using Icarus.Orchestrator.Application.Interfaces;
using Icarus.Orchestrator.Domain.Entities;
using Icarus.Orchestrator.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Icarus.Orchestrator.Application.Services;

public sealed class SourceService : ISourceService
{
    private readonly IDataSourceRepository _repository;
    private readonly ILogger<SourceService> _logger;

    public SourceService(IDataSourceRepository repository, ILogger<SourceService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<RegisterSourceResponse> RegisterAsync(RegisterSourceRequest request, CancellationToken ct = default)
    {
        _logger.LogInformation("Registering source {Name} of type {Type}", request.Name, request.SourceType);

        var source = DataSource.Create(request.Name, request.ConnectionString, request.SourceType);
        await _repository.AddAsync(source, ct);

        return new RegisterSourceResponse(source.Id, source.Name, source.Status.ToString());
    }

    public async Task BootstrapAsync(Guid sourceId, CancellationToken ct = default)
    {
        var source = await _repository.GetByIdAsync(sourceId, ct)
            ?? throw new InvalidOperationException($"Source {sourceId} not found");

        _logger.LogInformation("Bootstrapping source {Id} ({Name})", source.Id, source.Name);
        source.MarkBootstrapping();
        await _repository.UpdateAsync(source, ct);

        // In production, this would enqueue a background job.
        // For now, mark as ready after simulated work.
        source.MarkReady();
        await _repository.UpdateAsync(source, ct);

        _logger.LogInformation("Source {Id} bootstrapped successfully", source.Id);
    }
}
