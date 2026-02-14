using Icarus.Orchestrator.Domain.Entities;

namespace Icarus.Orchestrator.Application.Contracts;

public sealed record RegisterSourceRequest(
    string Name,
    string ConnectionString,
    SourceType SourceType);

public sealed record RegisterSourceResponse(
    Guid Id,
    string Name,
    string Status);
