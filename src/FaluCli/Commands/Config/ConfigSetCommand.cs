using Spectre.Console;

namespace Falu.Commands.Config;

internal class ConfigSetCommand : FaluCliCommand
{
    public ConfigSetCommand() : base("set", "Set a configuration value.")
    {
        this.AddArgument<string>(name: "key",
                                 description: "The configuration key.",
                                 configure: a => a.AcceptOnlyFromAmong("retries", "timeout", "workspace", "livemode"));

        this.AddArgument<string>(name: "value", description: "The configuration value.");
    }

    public override Task<int> ExecuteAsync(CliCommandExecutionContext context, CancellationToken cancellationToken)
    {
        var values = context.ConfigValues;
        var key = context.ParseResult.ValueForArgument<string>("key")!.ToLower();
        var value = context.ParseResult.ValueForArgument<string>("value")!;
        switch (key)
        {
            case "no-telemetry":
                values.NoTelemetry = bool.Parse(value);
                break;
            case "no-updates":
                values.NoUpdates = bool.Parse(value);
                break;
            case "retries":
                values.Retries = int.Parse(value);
                break;
            case "timeout":
                values.Timeout = int.Parse(value);
                break;
            case "workspace":
                values.DefaultWorkspaceId = value;
                break;
            case "livemode":
                values.DefaultLiveMode = bool.Parse(value);
                break;
            default:
                throw new NotSupportedException($"The key '{key}' is no supported yet.");
        }

        AnsiConsole.Write("Successfully set configuration '{0}={1}'.", key, value);

        return Task.FromResult(0);
    }
}
