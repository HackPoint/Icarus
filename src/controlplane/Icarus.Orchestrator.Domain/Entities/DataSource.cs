namespace Icarus.Orchestrator.Domain.Entities;

public sealed class DataSource
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string ConnectionString { get; private set; } = string.Empty;
    public SourceType SourceType { get; private set; }
    public SourceStatus Status { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? LastBootstrapUtc { get; private set; }

    private DataSource() { }

    public static DataSource Create(string name, string connectionString, SourceType sourceType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        return new DataSource
        {
            Id = Guid.NewGuid(),
            Name = name,
            ConnectionString = connectionString,
            SourceType = sourceType,
            Status = SourceStatus.Registered,
            CreatedAtUtc = DateTime.UtcNow
        };
    }

    public void MarkBootstrapping()
    {
        Status = SourceStatus.Bootstrapping;
    }

    public void MarkReady()
    {
        Status = SourceStatus.Ready;
        LastBootstrapUtc = DateTime.UtcNow;
    }

    public void MarkFailed()
    {
        Status = SourceStatus.Failed;
    }
}
