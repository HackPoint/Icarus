using FluentAssertions;
using Icarus.Orchestrator.Application.Contracts;
using Icarus.Orchestrator.Application.Services;
using Icarus.Orchestrator.Domain.Entities;
using Icarus.Orchestrator.Domain.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Icarus.Orchestrator.Tests.Unit.Application;

public class SourceServiceTests
{
    private readonly IDataSourceRepository _repository;
    private readonly SourceService _sut;

    public SourceServiceTests()
    {
        _repository = Substitute.For<IDataSourceRepository>();
        _sut = new SourceService(_repository, NullLogger<SourceService>.Instance);
    }

    [Fact]
    public async Task RegisterAsync_ShouldCreateAndPersistSource()
    {
        var request = new RegisterSourceRequest("test-db", "Host=localhost", SourceType.Postgres);

        var result = await _sut.RegisterAsync(request);

        result.Should().NotBeNull();
        result.Name.Should().Be("test-db");
        result.Status.Should().Be("Registered");
        result.Id.Should().NotBeEmpty();

        await _repository.Received(1).AddAsync(Arg.Any<DataSource>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task BootstrapAsync_WithExistingSource_ShouldMarkReady()
    {
        var source = DataSource.Create("test", "Host=localhost", SourceType.Postgres);
        _repository.GetByIdAsync(source.Id, Arg.Any<CancellationToken>()).Returns(source);

        await _sut.BootstrapAsync(source.Id);

        source.Status.Should().Be(SourceStatus.Ready);
        await _repository.Received(2).UpdateAsync(source, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task BootstrapAsync_WithNonExistentSource_ShouldThrow()
    {
        _repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((DataSource?)null);

        var act = () => _sut.BootstrapAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}
