using Icarus.Orchestrator.Infrastructure.Configuration;
using Icarus.Orchestrator.Workers.Jobs;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddIcarusInfrastructure();
builder.Services.AddHostedService<BootstrapWorker>();

var host = builder.Build();
host.Run();
