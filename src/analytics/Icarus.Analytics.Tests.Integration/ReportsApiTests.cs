using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Icarus.Analytics.Tests.Integration;

public class ReportsApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public ReportsApiTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Health_ShouldReturnOk()
    {
        var response = await _client.GetAsync("/health");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UsageReport_ShouldReturnDeterministicJson()
    {
        var response = await _client.GetAsync("/reports/usage");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("totalQueries");
        content.Should().Contain("1247");
    }

    [Fact]
    public async Task SourcesReport_ShouldReturnDeterministicJson()
    {
        var response = await _client.GetAsync("/reports/sources");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("engineering-docs");
    }
}
