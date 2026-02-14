using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Icarus.Connectors.CouchDb;

public interface ICouchDbClient
{
    Task<IReadOnlyList<JsonDocument>> GetAllDocumentsAsync(string database, CancellationToken ct = default);
    Task<JsonDocument?> GetDocumentAsync(string database, string docId, CancellationToken ct = default);
    Task CreateDatabaseAsync(string database, CancellationToken ct = default);
    Task<bool> DatabaseExistsAsync(string database, CancellationToken ct = default);
}

public sealed class CouchDbClient : ICouchDbClient, IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<CouchDbClient> _logger;

    public CouchDbClient(HttpClient httpClient, ILogger<CouchDbClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<IReadOnlyList<JsonDocument>> GetAllDocumentsAsync(string database, CancellationToken ct = default)
    {
        _logger.LogDebug("Fetching all documents from CouchDB database {Database}", database);

        try
        {
            var response = await _httpClient.GetAsync($"/{database}/_all_docs?include_docs=true", ct);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync(ct);
            var doc = JsonDocument.Parse(content);

            var rows = doc.RootElement.GetProperty("rows");
            var documents = new List<JsonDocument>();

            foreach (var row in rows.EnumerateArray())
            {
                if (row.TryGetProperty("doc", out var docElement))
                {
                    documents.Add(JsonDocument.Parse(docElement.GetRawText()));
                }
            }

            return documents;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "CouchDB not available, returning empty document list");
            return [];
        }
    }

    public async Task<JsonDocument?> GetDocumentAsync(string database, string docId, CancellationToken ct = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/{database}/{docId}", ct);
            if (!response.IsSuccessStatusCode) return null;

            var content = await response.Content.ReadAsStringAsync(ct);
            return JsonDocument.Parse(content);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Failed to get document {DocId} from {Database}", docId, database);
            return null;
        }
    }

    public async Task CreateDatabaseAsync(string database, CancellationToken ct = default)
    {
        try
        {
            var response = await _httpClient.PutAsync($"/{database}", null, ct);
            _logger.LogInformation("Created CouchDB database {Database}: {Status}", database, response.StatusCode);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Failed to create CouchDB database {Database}", database);
        }
    }

    public async Task<bool> DatabaseExistsAsync(string database, CancellationToken ct = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/{database}", ct);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public void Dispose() => _httpClient.Dispose();
}
