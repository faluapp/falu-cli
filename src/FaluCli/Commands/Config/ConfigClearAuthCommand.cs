using Spectre.Console;

namespace Falu.Commands.Config;

// internal class ConfigClearAllCommand : FaluCliCommand
// {
//     public ConfigClearAllCommand() : base("all", "Clear all configuration values by deleting the configuration file.")
//     {
//     }
// }

internal class ConfigClearAuthCommand : FaluCliCommand // TODO: remove this because clear logout does the same
{
    public ConfigClearAuthCommand() : base("auth", "Clear configuration values related to authentication.") { }

    public override Task<int> ExecuteAsync(CliCommandExecutionContext context, CancellationToken cancellationToken)
    {
        context.ConfigValues.Authentication = null;
        AnsiConsole.Write("Successfully removed all authentication configuration values.");

        return Task.FromResult(0);
    }
}
