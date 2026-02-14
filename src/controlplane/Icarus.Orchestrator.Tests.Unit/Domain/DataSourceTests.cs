using FluentAssertions;
using Icarus.Orchestrator.Domain.Entities;
using Xunit;

namespace Icarus.Orchestrator.Tests.Unit.Domain;

public class DataSourceTests
{
    [Fact]
    public void Create_WithValidInput_ShouldReturnRegisteredSource()
    {
        var source = DataSource.Create("test-source", "Host=localhost", SourceType.Postgres);

        source.Should().NotBeNull();
        source.Id.Should().NotBeEmpty();
        source.Name.Should().Be("test-source");
        source.ConnectionString.Should().Be("Host=localhost");
        source.SourceType.Should().Be(SourceType.Postgres);
        source.Status.Should().Be(SourceStatus.Registered);
        source.CreatedAtUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Create_WithEmptyName_ShouldThrow()
    {
        var act = () => DataSource.Create("", "Host=localhost", SourceType.Postgres);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithEmptyConnectionString_ShouldThrow()
    {
        var act = () => DataSource.Create("test", "", SourceType.Postgres);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void MarkBootstrapping_ShouldChangeStatus()
    {
        var source = DataSource.Create("test", "Host=localhost", SourceType.CouchDb);
        source.MarkBootstrapping();

        source.Status.Should().Be(SourceStatus.Bootstrapping);
    }

    [Fact]
    public void MarkReady_ShouldSetStatusAndTimestamp()
    {
        var source = DataSource.Create("test", "Host=localhost", SourceType.CouchDb);
        source.MarkBootstrapping();
        source.MarkReady();

        source.Status.Should().Be(SourceStatus.Ready);
        source.LastBootstrapUtc.Should().NotBeNull();
    }

    [Fact]
    public void MarkFailed_ShouldSetStatus()
    {
        var source = DataSource.Create("test", "Host=localhost", SourceType.CouchDb);
        source.MarkBootstrapping();
        source.MarkFailed();

        source.Status.Should().Be(SourceStatus.Failed);
    }
}
