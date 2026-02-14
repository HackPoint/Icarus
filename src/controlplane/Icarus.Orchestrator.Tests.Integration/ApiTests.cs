using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Icarus.Orchestrator.Application.Contracts;
using Icarus.Orchestrator.Domain.Entities;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Icarus.Orchestrator.Tests.Integration;

public class ApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public ApiTests(WebApplicationFactory<Program> factory)
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
    public async Task RegisterSource_ShouldReturnCreated()
    {
        var request = new RegisterSourceRequest("test-source", "Host=localhost", SourceType.CouchDb);

        var response = await _client.PostAsJsonAsync("/sources/register", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<RegisterSourceResponse>();
        result.Should().NotBeNull();
        result!.Name.Should().Be("test-source");
    }

    [Fact]
    public async Task ChatQuery_ShouldReturnDeterministicResponse()
    {
        var request = new ChatRequest("What is Icarus?");

        var response = await _client.PostAsJsonAsync("/chat/query", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ChatResponse>();
        result.Should().NotBeNull();
        result!.Answer.Should().NotBeNullOrEmpty();
        result.Citations.Should().NotBeEmpty();
    }

    [Fact]
    public async Task ChatStream_ShouldReturnSseEvents()
    {
        var response = await _client.GetAsync("/chat/stream?query=test");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("text/event-stream");

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("event: token");
        content.Should().Contain("event: final");
    }
}
