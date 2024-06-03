using Spectre.Console;

namespace Falu.Commands.Config;

internal class ConfigShowCommand : Command
{
    public ConfigShowCommand() : base("show", "Show present configuration values.")
    {
        this.SetHandler(Handle);
    }

    private static void Handle(InvocationContext context)
    {
        var values = context.GetConfigValues();
        var data = new Dictionary<string, object?>
        {
            ["no-telemetry"] = values.NoTelemetry,
            ["no-updates"] = values.NoUpdates,
            ["retries"] = values.Retries,
            ["timeout"] = $"{values.Timeout} seconds",
            ["workspace"] = values.DefaultWorkspaceId,
            ["livemode"] = values.DefaultLiveMode,
        }.RemoveDefaultAndEmpty();

        if (data.Count == 0)
        {
            AnsiConsole.Write("Configuration values are empty or only contain sensitive information.");
        }
        else
        {
            var table = new Table().AddColumn("Key")
                                   .AddColumn(new TableColumn("Value").Centered());

            foreach (var (key, value) in data) table.AddRow(new Markup(key), new Markup($"{value}"));

            AnsiConsole.Write(table);
        }
    }
}
