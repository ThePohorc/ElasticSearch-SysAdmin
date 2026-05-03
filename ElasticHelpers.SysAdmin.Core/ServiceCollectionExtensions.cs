using Microsoft.Extensions.DependencyInjection;

namespace ElasticHelpers.SysAdmin.Core;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddElasticsearchCore(
        this IServiceCollection services,
        ElasticsearchSettings settings)
    {
        services.AddSingleton(settings);
        services.AddSingleton<IElasticsearchService, ElasticsearchService>();
        return services;
    }
}
