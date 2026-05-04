using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using Elastic.Clients.Elasticsearch;
using Elastic.Transport;
using ElasticHelpers.SysAdmin.Core.Models;

namespace ElasticHelpers.SysAdmin.Core;

public class ElasticsearchService : IElasticsearchService
{
    private readonly ElasticsearchClient _client;
    private readonly HttpClient _http;
    private readonly string _baseUrl;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public ElasticsearchService(ElasticsearchSettings settings)
    {
        var clientSettings = new ElasticsearchClientSettings(new Uri(settings.Url))
            .Authentication(new ApiKey(settings.ApiKey));
        _client = new ElasticsearchClient(clientSettings);

        _baseUrl = settings.Url.TrimEnd('/');
        _http = new HttpClient();
        _http.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("ApiKey", settings.ApiKey);
    }

    public async Task<bool> PingAsync()
    {
        var response = await _client.PingAsync();
        return response.IsValidResponse;
    }

    public async Task<IReadOnlyList<IndexSizeInfo>> GetIndexesSizeInfoAsync()
    {
        var url = $"{_baseUrl}/_cat/indices?format=json" +
                  "&h=health,status,index,pri,rep,docs.count,store.size&s=health:desc";

        var json = await _http.GetStringAsync(url);

        var records = JsonSerializer.Deserialize<List<CatIndexRecord>>(json, JsonOptions)
                      ?? [];

        return records
            .Select(r => new IndexSizeInfo(
                r.Health ?? "",
                r.Status ?? "",
                r.Index ?? "",
                r.Pri ?? "",
                r.Rep ?? "",
                r.DocsCount ?? "",
                r.StoreSize ?? ""))
            .ToList();
    }

    public async Task<IReadOnlyList<IndexSizeInfo>> GetIndexInfoAsync(string indexName)
    {
        var indexNames = await ResolveUnderlyingIndicesAsync(indexName);

        if (indexNames.Count == 0)
            return [];

        var joined = string.Join(",", indexNames);
        var url = $"{_baseUrl}/_cat/indices/{joined}?format=json" +
                  "&h=health,status,index,pri,rep,docs.count,store.size&s=index:asc";

        var json = await _http.GetStringAsync(url);
        var records = JsonSerializer.Deserialize<List<CatIndexRecord>>(json, JsonOptions) ?? [];

        return records
            .Select(r => new IndexSizeInfo(
                r.Health ?? "",
                r.Status ?? "",
                r.Index ?? "",
                r.Pri ?? "",
                r.Rep ?? "",
                r.DocsCount ?? "",
                r.StoreSize ?? ""))
            .ToList();
    }

    private async Task<List<string>> ResolveUnderlyingIndicesAsync(string indexName)
    {
        // Try alias first
        var aliasResponse = await _http.GetAsync($"{_baseUrl}/_alias/{indexName}");
        if (aliasResponse.IsSuccessStatusCode)
        {
            var json = await aliasResponse.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var names = doc.RootElement.EnumerateObject()
                .Select(p => p.Name)
                .OrderBy(n => n)
                .ToList();
            if (names.Count > 0)
                return names;
        }

        // Fall back to data stream
        var dsResponse = await _http.GetAsync($"{_baseUrl}/_data_stream/{indexName}");
        if (dsResponse.IsSuccessStatusCode)
        {
            var json = await dsResponse.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement
                .GetProperty("data_streams")[0]
                .GetProperty("indices")
                .EnumerateArray()
                .Select(i => i.GetProperty("index_name").GetString() ?? "")
                .Where(n => n.Length > 0)
                .OrderBy(n => n)
                .ToList();
        }

        throw new InvalidOperationException($"No alias or data stream found for '{indexName}'.");
    }

    private sealed record CatIndexRecord(
        [property: JsonPropertyName("health")]     string? Health,
        [property: JsonPropertyName("status")]     string? Status,
        [property: JsonPropertyName("index")]      string? Index,
        [property: JsonPropertyName("pri")]        string? Pri,
        [property: JsonPropertyName("rep")]        string? Rep,
        [property: JsonPropertyName("docs.count")] string? DocsCount,
        [property: JsonPropertyName("store.size")] string? StoreSize
    );
}
