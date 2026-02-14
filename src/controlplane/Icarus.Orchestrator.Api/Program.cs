using Icarus.Orchestrator.Infrastructure.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHealthChecks();
builder.Services.AddIcarusInfrastructure();

var app = builder.Build();

app.MapHealthChecks("/health");
app.MapHealthChecks("/alive");
app.MapControllers();

app.Run();

// Enable WebApplicationFactory for integration tests
public partial class Program { }
