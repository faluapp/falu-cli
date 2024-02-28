namespace Falu.Commands.Config;

public class ConfigShowCommand : Command
{
    public ConfigShowCommand() : base("show", "Show present configuration values.")
    {
    }
}

public class ConfigSetCommand : Command
{
    public ConfigSetCommand() : base("set", "Set a configuration value.")
    {
        this.AddArgument<string>(name: "key",
                                 description: "The configuration key.",
                                 configure: a => a.FromAmong("retries", "timeout", "workspace", "livemode"));

        this.AddArgument<string>(name: "value", description: "The configuration value.");
    }
}

public class ConfigClearAllCommand : Command
{
    public ConfigClearAllCommand() : base("all", "Clear all configuration values by deleting the configuration file.")
    {
    }
}

public class ConfigClearAuthCommand : Command
{
    public ConfigClearAuthCommand() : base("auth", "Clear configuration values related to authentication.")
    {
    }
}
