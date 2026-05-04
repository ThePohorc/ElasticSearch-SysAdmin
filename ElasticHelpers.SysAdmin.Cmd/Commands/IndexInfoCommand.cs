using System.ComponentModel;
using ElasticHelpers.SysAdmin.Core;
using ElasticHelpers.SysAdmin.Core.Models;
using Spectre.Console;
using Spectre.Console.Cli;

namespace ElasticHelpers.SysAdmin.Cmd.Commands;

public sealed class IndexInfoCommand : AsyncCommand<IndexInfoCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [CommandArgument(0, "<index>")]
        [Description("Index name or alias whose rolled-over underlying indices to list.")]
        public string IndexName { get; init; } = string.Empty;
    }

    private readonly IElasticsearchService _elasticsearch;

    public IndexInfoCommand(IElasticsearchService elasticsearch) =>
        _elasticsearch = elasticsearch;

    protected override async Task<int> ExecuteAsync(
        CommandContext context,
        Settings settings,
        CancellationToken cancellationToken)
    {
        var indices = await _elasticsearch.GetIndexInfoAsync(settings.IndexName);

        if (indices.Count == 0)
        {
            AnsiConsole.MarkupLine($"[yellow]No underlying indices found for '{settings.IndexName}'.[/]");
            return 0;
        }

        AnsiConsole.Write(BuildTable(indices));
        return 0;
    }

    private static Table BuildTable(IReadOnlyList<IndexSizeInfo> indices)
    {
        var table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn(new TableColumn("#").RightAligned())
            .AddColumn("Health")
            .AddColumn("Status")
            .AddColumn("Index")
            .AddColumn(new TableColumn("Pri").RightAligned())
            .AddColumn(new TableColumn("Rep").RightAligned())
            .AddColumn(new TableColumn("Docs Count").RightAligned())
            .AddColumn(new TableColumn("Store Size").RightAligned());

        foreach (var (idx, i) in indices.Select((x, i) => (x, i + 1)))
        {
            var healthMarkup = idx.Health switch
            {
                "green"  => "[green]green[/]",
                "yellow" => "[yellow]yellow[/]",
                "red"    => "[red]red[/]",
                _        => idx.Health
            };

            table.AddRow(i.ToString(), healthMarkup, idx.Status, idx.Index,
                         idx.Pri, idx.Rep, idx.DocsCount, idx.StoreSize);
        }

        return table;
    }
}
