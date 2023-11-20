using Falu.Config;
using Spectre.Console;

namespace Falu.Commands.Config;

internal class ConfigCommandHandler(IConfigValuesProvider configValuesProvider) : ICommandHandler
{
    int ICommandHandler.Invoke(InvocationContext context) => throw new NotImplementedException();

    public async Task<int> InvokeAsync(InvocationContext context)
    {
        var cancellationToken = context.GetCancellationToken();

        switch (context.ParseResult.CommandResult.Command)
        {
            case ConfigShowCommand:
                {
                    var values = await configValuesProvider.GetConfigValuesAsync(cancellationToken);

                    var data = new Dictionary<string, object?>
                    {
                        ["skip-update-check"] = values.SkipUpdateChecks,
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

                    break;
                }
            case ConfigSetCommand:
                {
                    var values = await configValuesProvider.GetConfigValuesAsync(cancellationToken);

                    var key = context.ParseResult.ValueForArgument<string>("key")!.ToLower();
                    var value = context.ParseResult.ValueForArgument<string>("value")!;
                    switch (key)
                    {
                        case "skip-update-check":
                            values.SkipUpdateChecks = bool.Parse(value);
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
                    await configValuesProvider.SaveConfigValuesAsync(cancellationToken);
                    AnsiConsole.Write("Successfully set configuration '{0}={1}'.", key, value);
                    break;
                }
            case ConfigClearAuthCommand:
                {
                    await configValuesProvider.ClearAuthenticationAsync(cancellationToken);
                    AnsiConsole.Write("Successfully removed all authentication configuration values.");
                    break;
                }
            case ConfigClearAllCommand:
                {
                    configValuesProvider.ClearAll();
                    AnsiConsole.Write("Successfully removed all configuration values and the configuration file.");
                    break;
                }
        }

        return 0;
    }
}
