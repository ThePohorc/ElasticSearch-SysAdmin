using System.ComponentModel;
using System.Text;
using ElasticHelpers.SysAdmin.Core;
using ElasticHelpers.SysAdmin.Core.Models;
using Spectre.Console;
using Spectre.Console.Cli;

namespace ElasticHelpers.SysAdmin.Cmd.Commands;

public sealed class GetIndexesSizeInfoCommand : AsyncCommand<GetIndexesSizeInfoCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [CommandOption("--csv <FILE>")]
        [Description("Export results to the specified CSV file path.")]
        public string? CsvOutput { get; init; }
    }

    private readonly IElasticsearchService _elasticsearch;

    public GetIndexesSizeInfoCommand(IElasticsearchService elasticsearch) =>
        _elasticsearch = elasticsearch;

    protected override async Task<int> ExecuteAsync(
        CommandContext context,
        Settings settings,
        CancellationToken cancellationToken)
    {
        var indices = await _elasticsearch.GetIndexesSizeInfoAsync();

        RenderTable(indices);

        if (settings.CsvOutput is not null)
            await ExportCsvAsync(indices, settings.CsvOutput, cancellationToken);

        return 0;
    }

    private static void RenderTable(IReadOnlyList<IndexSizeInfo> indices)
    {
        var table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn(new TableColumn("#").RightAligned())
            .AddColumn("Health")
            .AddColumn("Status")
            .AddColumn("Index")
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

            table.AddRow(i.ToString(), healthMarkup, idx.Status, idx.Index, idx.DocsCount, idx.StoreSize);
        }

        AnsiConsole.Write(table);
    }

    private static async Task ExportCsvAsync(
        IReadOnlyList<IndexSizeInfo> indices,
        string path,
        CancellationToken cancellationToken)
    {
        var csv = new StringBuilder();
        csv.AppendLine("health,status,index,docs.count,store.size");

        foreach (var idx in indices)
            csv.AppendLine($"{idx.Health},{idx.Status},{EscapeCsv(idx.Index)},{idx.DocsCount},{idx.StoreSize}");

        await File.WriteAllTextAsync(path, csv.ToString(), cancellationToken);
        AnsiConsole.MarkupLine($"[grey]Exported {indices.Count} rows to {path}[/]");
    }

    private static string EscapeCsv(string value) =>
        value.Contains(',') || value.Contains('"') || value.Contains('\n')
            ? $"\"{value.Replace("\"", "\"\"")}\""
            : value;
}
