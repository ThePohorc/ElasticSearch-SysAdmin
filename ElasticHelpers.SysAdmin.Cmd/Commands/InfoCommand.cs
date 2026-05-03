using System.Reflection;
using Spectre.Console;
using Spectre.Console.Cli;

namespace ElasticHelpers.SysAdmin.Cmd.Commands;

public sealed class InfoCommand : Command
{
    protected override int Execute(CommandContext context, CancellationToken cancellationToken)
    {
        var buildDate = typeof(InfoCommand).Assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(a => a.Key == "BuildDate")?.Value ?? "Unknown";

        AnsiConsole.WriteLine(buildDate);
        return 0;
    }
}
