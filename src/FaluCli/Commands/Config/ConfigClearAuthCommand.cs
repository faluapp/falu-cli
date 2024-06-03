using Spectre.Console;

namespace Falu.Commands.Config;

// internal class ConfigClearAllCommand : Command
// {
//     public ConfigClearAllCommand() : base("all", "Clear all configuration values by deleting the configuration file.")
//     {
//     }
// }

internal class ConfigClearAuthCommand : Command
{
    public ConfigClearAuthCommand() : base("auth", "Clear configuration values related to authentication.")
    {
        this.SetHandler(Handle);
    }

    private static void Handle(InvocationContext context)
    {
        var values = context.GetConfigValues();
        values.Authentication = null;
        AnsiConsole.Write("Successfully removed all authentication configuration values.");
    }
}
