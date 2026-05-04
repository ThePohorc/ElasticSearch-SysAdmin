using ElasticHelpers.SysAdmin.Core.Models;

namespace ElasticHelpers.SysAdmin.Core;

public interface IElasticsearchService
{
    Task<bool> PingAsync();
    Task<IReadOnlyList<IndexSizeInfo>> GetIndexesSizeInfoAsync();
    Task<IReadOnlyList<IndexSizeInfo>> GetIndexInfoAsync(string indexName);
}
