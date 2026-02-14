using Icarus.Orchestrator.Domain.Interfaces;
using Icarus.Orchestrator.Domain.Entities;

namespace Icarus.Orchestrator.Workers.Jobs;

public sealed class BootstrapWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BootstrapWorker> _logger;
    private readonly TimeSpan _pollInterval = TimeSpan.FromSeconds(30);

    public BootstrapWorker(IServiceScopeFactory scopeFactory, ILogger<BootstrapWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("BootstrapWorker started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var repo = scope.ServiceProvider.GetRequiredService<IDataSourceRepository>();

                var sources = await repo.GetAllAsync(stoppingToken);
                var pendingBootstraps = sources.Where(s => s.Status == SourceStatus.Bootstrapping);

                foreach (var source in pendingBootstraps)
                {
                    _logger.LogInformation("Processing bootstrap for source {Id}", source.Id);

                    // Simulated bootstrap work
                    source.MarkReady();
                    await repo.UpdateAsync(source, stoppingToken);

                    _logger.LogInformation("Source {Id} bootstrap completed", source.Id);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error in BootstrapWorker");
            }

            await Task.Delay(_pollInterval, stoppingToken);
        }
    }
}
