using Spectre.Console;

namespace Falu.Commands.Config;

internal class ConfigSetCommand : Command
{
    public ConfigSetCommand() : base("set", "Set a configuration value.")
    {
        this.AddArgument<string>(name: "key",
                                 description: "The configuration key.",
                                 configure: a => a.FromAmong("retries", "timeout", "workspace", "livemode"));

        this.AddArgument<string>(name: "value", description: "The configuration value.");

        this.SetHandler(Handle);
    }

    private static void Handle(InvocationContext context)
    {
        var values = context.GetConfigValues();
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
    }
}
