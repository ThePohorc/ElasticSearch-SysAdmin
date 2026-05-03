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
