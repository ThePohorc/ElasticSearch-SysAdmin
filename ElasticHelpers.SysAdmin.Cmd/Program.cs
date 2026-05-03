using ElasticHelpers.SysAdmin.Cmd.Commands;
using ElasticHelpers.SysAdmin.Cmd.Infrastructure;
using ElasticHelpers.SysAdmin.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console.Cli;

IConfiguration config = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
    .AddUserSecrets<Program>(optional: true)
    .AddEnvironmentVariables()
    .Build();

var services = new ServiceCollection();

services.AddElasticsearchCore(new ElasticsearchSettings
{
    Url = config["Elasticsearch:Url"] ?? throw new InvalidOperationException("Elasticsearch:Url is required"),
    ApiKey = config["Elasticsearch:ApiKey"] ?? string.Empty,
});

var app = new CommandApp(new TypeRegistrar(services));

app.Configure(cfg =>
{
    cfg.AddCommand<PingCommand>("ping")
       .WithDescription("Tests if the Elasticsearch cluster is reachable.");
});

return app.Run(args);
