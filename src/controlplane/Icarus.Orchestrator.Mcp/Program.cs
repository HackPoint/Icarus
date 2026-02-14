using Icarus.Orchestrator.Infrastructure.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddHealthChecks();
builder.Services.AddIcarusInfrastructure();

var app = builder.Build();

app.MapHealthChecks("/health");
app.MapControllers();

app.Run();
