namespace ElasticHelpers.SysAdmin.Core;

public interface IElasticsearchService
{
    Task<bool> PingAsync();
}
