namespace Falu.Commands.Config;

internal class ConfigCommand : Command
{
    public ConfigCommand() : base("config", "Manage configuration for the CLI.")
    {
        Add(new ConfigShowCommand());
        Add(new ConfigSetCommand());
        Add(new Command("clear", "Clear configuration for the CLI.")
        {
            // new ConfigClearAllCommand(),
            new ConfigClearAuthCommand(),
        });
    }
}
