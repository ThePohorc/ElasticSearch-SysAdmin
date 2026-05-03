using Elastic.Clients.Elasticsearch;
using Elastic.Transport;

namespace ElasticHelpers.SysAdmin.Core;

public class ElasticsearchService : IElasticsearchService
{
    private readonly ElasticsearchClient _client;

    public ElasticsearchService(ElasticsearchSettings settings)
    {
        var clientSettings = new ElasticsearchClientSettings(new Uri(settings.Url))
            .Authentication(new ApiKey(settings.ApiKey));

        _client = new ElasticsearchClient(clientSettings);
    }

    public async Task<bool> PingAsync()
    {
        var response = await _client.PingAsync();
        return response.IsValidResponse;
    }
}
