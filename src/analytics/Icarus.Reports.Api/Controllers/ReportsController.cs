using Microsoft.AspNetCore.Mvc;

namespace Icarus.Reports.Api.Controllers;

[ApiController]
[Route("reports")]
public sealed class ReportsController : ControllerBase
{
    [HttpGet("usage")]
    public IActionResult GetUsageReport()
    {
        // Deterministic stub — in production reads from ClickHouse
        return Ok(new
        {
            reportType = "usage",
            generatedAt = DateTime.UtcNow,
            period = new { from = "2025-01-01", to = "2025-12-31" },
            metrics = new
            {
                totalQueries = 1_247,
                uniqueUsers = 42,
                avgResponseTimeMs = 312,
                p95ResponseTimeMs = 890,
                tokensGenerated = 523_000
            }
        });
    }

    [HttpGet("sources")]
    public IActionResult GetSourcesReport()
    {
        return Ok(new
        {
            reportType = "sources",
            generatedAt = DateTime.UtcNow,
            sources = new[]
            {
                new { name = "engineering-docs", type = "CouchDb", documentCount = 1520, status = "Ready" },
                new { name = "product-wiki", type = "Postgres", documentCount = 830, status = "Ready" },
                new { name = "research-papers", type = "S3", documentCount = 256, status = "Ready" }
            }
        });
    }

    [HttpGet("queries")]
    public IActionResult GetQueriesReport()
    {
        return Ok(new
        {
            reportType = "queries",
            generatedAt = DateTime.UtcNow,
            topQueries = new[]
            {
                new { query = "How to configure SSO?", count = 89, avgScore = 0.92 },
                new { query = "Deployment architecture", count = 67, avgScore = 0.88 },
                new { query = "API rate limits", count = 54, avgScore = 0.85 }
            }
        });
    }
}
