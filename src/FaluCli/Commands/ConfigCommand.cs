using Falu.Config;
using Spectre.Console;

namespace Falu.Commands;

internal class ConfigCommand : CliCommand
{
    public ConfigCommand() : base("config", "Manage configuration for the CLI.")
    {
        Add(new ConfigShowCommand());
        Add(new ConfigSetCommand());
        Add(new ConfigUnsetCommand());
    }
}

internal abstract class AbstractConfigCommand(string name, string? description = null) : FaluCliCommand(name, description)
{
    /// <summary>Represents a registration for a configuration option.</summary>
    /// <param name="name">The name of the configuration option.</param>
    /// <param name="description">The description of the configuration option.</param>
    protected abstract class ConfigRegistration(string name, string description)
    {
        /// <summary>Gets the name of the configuration option.</summary>
        public string Name { get; } = name;

        /// <summary>Gets the description of the configuration option.</summary>
        public string Description { get; } = description;

        /// <summary>Gets the value of the configuration option from an instance of <see cref="ConfigValues"/>.</summary>
        /// <param name="values">The <see cref="ConfigValues"/> instance from which to get the value.</param>
        /// <returns></returns>
        public abstract object? GetValue(ConfigValues values);

        /// <summary>Validates the value of the configuration option.</summary>
        /// <param name="context">The current <see cref="ConfigValues"/> instance.</param>
        /// <param name="value">The value to validate.</param>
        /// <returns>
        /// <see langword="null"/> when the value is valid; otherwise, it returns an error message.
        /// </returns>
        public abstract string? Validate(ConfigValues values, string value);

        /// <summary>Sets the value of the configuration option in an instance of <see cref="ConfigValues"/>.</summary>
        /// <param name="context">The <see cref="ConfigValues"/> instance in which to set the value.</param>
        /// <param name="value">The value to set.</param>
        public abstract void SetValue(ConfigValues values, string value);

        /// <summary>Clears the value of the configuration option in an instance of <see cref="ConfigValues"/>.</summary>
        /// <param name="context">The <see cref="ConfigValues"/> instance in which to set the value.</param>
        public abstract void ClearValue(ConfigValues values);
    }

    /// <summary>Represents a registration for a configuration option.</summary>
    /// <param name="name">The name of the configuration option.</param>
    /// <param name="description">The description of the configuration option.</param>
    /// <param name="validator">
    /// A function that validates the value of the configuration option.
    /// This function returns <see langword="null"/> when the value is valid; otherwise, it returns an error message.
    /// </param>
    /// <param name="getter">
    /// A function that gets the value of the configuration option from an instance of <see cref="ConfigValues"/>.
    /// </param>
    /// <param name="setter">
    /// A function that sets the value of the configuration option in an instance of <see cref="ConfigValues"/>.
    /// </param>
    /// <param name="clear">
    /// A function that clears the value of the configuration option in an instance of <see cref="ConfigValues"/>.
    /// </param>
    protected class ConfigRegistration<T>(string name,
                                          string description,
                                          Func<ConfigValues, T> getter,
                                          Func<ConfigValues, string, string?> validator,
                                          Action<ConfigValues, string> setter,
                                          Action<ConfigValues> clear)
        : ConfigRegistration(name, description)
    {
        public override object? GetValue(ConfigValues values) => getter(values);
        public override string? Validate(ConfigValues values, string value) => validator(values, value);
        public override void SetValue(ConfigValues values, string value) => setter(values, value);
        public override void ClearValue(ConfigValues values) => clear(values);
    }

    protected static readonly ConfigRegistration[] ConfigRegistrations =
    [
        new ConfigRegistration<bool>(
            name: "no-telemetry",
            description: "Whether to disable collection of usage telemetry. (true|false)",
            getter: cv => cv.NoTelemetry,
            validator: (cv, value) => bool.TryParse(value, out _) ? null : "The value must be a boolean.",
            setter: (cv, value) => cv.NoTelemetry = bool.Parse(value),
            clear: cv => cv.NoTelemetry = false),

        new ConfigRegistration<bool>(
            name: "no-updates",
            description: "Whether to disable check for updates. (true|false)\r\nBy default we check them once every 24 hours",
            getter: cv => cv.NoUpdates,
            validator: (cv, value) => bool.TryParse(value, out _) ? null : "The value must be a boolean.",
            setter: (cv, value) => cv.NoUpdates = bool.Parse(value),
            clear: cv => cv.NoUpdates = false),

        new ConfigRegistration<int>(
            name: "retries",
            description: "The number of retries to perform for outgoing requests.",
            getter: cv => cv.Retries,
            validator: (cv, value) =>
            {
                if (!int.TryParse(value, out var i)) return (string?)"The value must be an integer.";
                if (i < 0 || i > 10) return "The value must be between 0 and 10.";
                return null;
            },
            setter: (cv, value) => cv.Retries = int.Parse(value),
            clear: cv => cv.Retries = ConfigValues.DefaultRetries),

        new ConfigRegistration<int>(
            name: "timeout",
            description: "The timeout for outgoing requests in seconds.",
            getter: cv => cv.Timeout,
            validator: (cv, value) =>
            {
                if (!int.TryParse(value, out var i)) return (string?)"The value must be an integer.";
                if (i < 10 || i > 300) return "The value must be between 10 and 300.";
                return null;
            },
            setter: (cv, value) => cv.Timeout = int.Parse(value),
            clear: cv => cv.Timeout = ConfigValues.DefaultTimeout),

        new ConfigRegistration<string?>(
            name: "workspace",
            description: "The default workspace identifier."
                       + "\r\nExample: wksp_610010be9228355f14ce6e08"
                       + "\r\nYou can also set this using the name of the workspace."
                       + "\r\nThis is used when there is no value set and not authenticating via an API Key.",
            getter: cv => cv.DefaultWorkspaceId,
            validator: (cv, value) =>
            {
                return !cv.TryGetWorkspaceId(value, out _)
                    ? "The value must be a valid workspace ID or name. If the value is correct, try sync workspaces via `falu workspaces list --refresh'"
                    : null;
            },
            setter: (cv, value) => cv.DefaultWorkspaceId = cv.GetRequiredWorkspace(value).Id,
            clear: cv => cv.DefaultWorkspaceId = null),

        new ConfigRegistration<bool?>(
            name: "live-mode",
            description: "The default live mode. (true|false)\r\nThis is used when there is no value set and not authenticating via an API Key.",
            getter: cv => cv.DefaultLiveMode,
            validator: (cv, value) => value is null || bool.TryParse(value, out _) ? null : "The value must be a boolean.",
            setter: (cv, value) => cv.DefaultLiveMode = value is null ? null : bool.Parse(value),
            clear: cv => cv.DefaultLiveMode = null),
    ];

    protected static string[] ConfigNames = ConfigRegistrations.Select(r => r.Name).ToArray();
    protected static ConfigRegistration FindRegistration(string name) => ConfigRegistrations.First(r => string.Equals(r.Name, name, StringComparison.OrdinalIgnoreCase));
}

internal class ConfigShowCommand() : AbstractConfigCommand("show", "Show present configuration values.")
{
    public override Task<int> ExecuteAsync(CliCommandExecutionContext context, CancellationToken cancellationToken)
    {
        var table = new Table().AddColumn("Name")
                               .AddColumn("Description")
                               .AddColumn("Value");

        table.ShowRowSeparators = true; // improve readability for multiple-line cells

        foreach (var registration in ConfigRegistrations)
        {
            var name = registration.Name;
            var description = registration.Description;
            var value = registration.GetValue(context.ConfigValues);
            var stringValue = value switch
            {
                DateTimeOffset dto => dto.ToString("R"),
                DateTime dt => dt.ToString("R"),
                TimeSpan ts => ts.ToReadableString(),
                bool b => b.ToString().ToLowerInvariant(),

                _ => value?.ToString(),
            } ?? "<null>";

            table.AddRow(new Markup(name), new Markup(description), new Markup(stringValue));
        }

        AnsiConsole.Write(table);

        return Task.FromResult(0);
    }
}

internal class ConfigSetCommand : AbstractConfigCommand
{
    public ConfigSetCommand() : base("set", "Set a configuration value.")
    {
        this.AddArgument<string>(name: "name", description: "The configuration name.", configure: a => a.AcceptOnlyFromAmong(ConfigNames));
        this.AddArgument<string>(name: "value", description: "The configuration value.");
    }

    public override Task<int> ExecuteAsync(CliCommandExecutionContext context, CancellationToken cancellationToken)
    {
        var configValues = context.ConfigValues;

        var name = context.ParseResult.ValueForArgument<string>("name")!.ToLower();
        var value = context.ParseResult.ValueForArgument<string>("value")!;

        // find registration and validate the value
        var registration = FindRegistration(name);
        var error = registration.Validate(configValues, value);
        if (error is not null)
        {
            AnsiConsole.MarkupLine(SpectreFormatter.ColouredRed(error));
            return Task.FromResult(-1);
        }

        // set the value
        registration.SetValue(configValues, value);

        AnsiConsole.WriteLine("Successfully set configuration for '{0}'.", name);
        return Task.FromResult(0);
    }
}

internal class ConfigUnsetCommand : AbstractConfigCommand
{
    public ConfigUnsetCommand() : base("unset", "Unset a configuration value.")
    {
        this.AddArgument<string>(name: "name", description: "The configuration name.", configure: a => a.AcceptOnlyFromAmong(ConfigNames));
    }

    public override Task<int> ExecuteAsync(CliCommandExecutionContext context, CancellationToken cancellationToken)
    {
        var name = context.ParseResult.ValueForArgument<string>("name")!.ToLower();

        var registration = FindRegistration(name);
        registration.ClearValue(context.ConfigValues);

        AnsiConsole.WriteLine("Successfully unset configuration for '{0}'.", name);
        return Task.FromResult(0);
    }
}
