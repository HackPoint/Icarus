namespace Icarus.Orchestrator.Domain.Entities;

public enum SourceStatus
{
    Registered = 0,
    Bootstrapping = 1,
    Ready = 2,
    Failed = 3
}
