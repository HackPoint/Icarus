using Icarus.Orchestrator.Application.Interfaces;
using Icarus.Orchestrator.Application.Services;
using Icarus.Orchestrator.Domain.Interfaces;
using Icarus.Orchestrator.Infrastructure.ExternalServices;
using Icarus.Orchestrator.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace Icarus.Orchestrator.Infrastructure.Configuration;

public static class ServiceRegistration
{
    public static IServiceCollection AddIcarusInfrastructure(this IServiceCollection services)
    {
        // Persistence (in-memory for dev; swap for EFCore + Postgres in production)
        services.AddSingleton<IDataSourceRepository, InMemoryDataSourceRepository>();

        // External services (stubs for dev; swap for real clients in production)
        services.AddSingleton<IVectorStore, StubVectorStore>();
        services.AddSingleton<IEmbeddingService, StubEmbeddingService>();
        services.AddSingleton<ILlmService, StubLlmService>();

        // Application services
        services.AddScoped<ISourceService, SourceService>();
        services.AddScoped<IChatService, ChatService>();

        return services;
    }
}
