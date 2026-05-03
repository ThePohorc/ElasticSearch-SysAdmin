using ElasticHelpers.SysAdmin.Core;
using Spectre.Console;
using Spectre.Console.Cli;

namespace ElasticHelpers.SysAdmin.Cmd.Commands;

public sealed class PingCommand : AsyncCommand
{
    private readonly IElasticsearchService _elasticsearch;

    public PingCommand(IElasticsearchService elasticsearch) => _elasticsearch = elasticsearch;

    protected override async Task<int> ExecuteAsync(CommandContext context, CancellationToken cancellationToken)
    {
        var reachable = await _elasticsearch.PingAsync();
        AnsiConsole.WriteLine(reachable ? "true" : "false");
        return 0;
    }
}
