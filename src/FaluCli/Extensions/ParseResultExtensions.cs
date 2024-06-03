using System.Diagnostics.CodeAnalysis;

namespace System.CommandLine.Parsing;

/// <summary>Extensions for <see cref="ParseResult"/>.</summary>
internal static class ParseResultExtensions
{
    // These extensions exist here as a workaround resulting from changes in beta2
    // A better way of binding should be sort out

    public static T? ValueForOption<T>(this ParseResult result, string alias)
    {
        if (string.IsNullOrWhiteSpace(alias))
        {
            throw new ArgumentException($"'{nameof(alias)}' cannot be null or whitespace.", nameof(alias));
        }

        var opt = result.CommandResult.Command.FindOption<T>(alias);
        return opt is null ? default : result.GetValue(opt);
    }

    public static T? ValueForArgument<T>(this ParseResult result, string alias)
    {
        if (string.IsNullOrWhiteSpace(alias))
        {
            throw new ArgumentException($"'{nameof(alias)}' cannot be null or whitespace.", nameof(alias));
        }

        var arg = result.CommandResult.Command.FindArgument<T>(alias);
        return arg is null ? default : result.GetValue(arg);
    }

    private static CliOption<T>? FindOption<T>(this CliCommand command, string alias)
    {
        ArgumentNullException.ThrowIfNull(command, nameof(command));
        ArgumentNullException.ThrowIfNull(alias, nameof(alias));

        var opt = command.Options.FirstOrDefault(o => o.Name == alias || o.Aliases.Contains(alias));
        if (opt is not null && opt is CliOption<T> opt_t) return opt_t;

        var parent = command.Parents.OfType<CliCommand>().SingleOrDefault();
        if (parent is not null) return FindOption<T>(parent, alias);
        return null;
    }

    private static CliArgument<T>? FindArgument<T>(this CliCommand command, string name)
    {
        ArgumentNullException.ThrowIfNull(command, nameof(command));
        ArgumentNullException.ThrowIfNull(name, nameof(name));

        var arg = command.Arguments.FirstOrDefault(o => o.Name.Equals(name));
        if (arg is not null && arg is CliArgument<T> arg_t) return arg_t;

        var parent = command.Parents.OfType<CliCommand>().SingleOrDefault();
        if (parent is not null) return FindArgument<T>(parent, name);
        return null;
    }

    public static bool IsVerboseEnabled(this ParseResult result) => result.ValueForOption<bool>("--verbose");
    public static bool IsNoTelemetry(this ParseResult result) => result.ValueForOption<bool>("--no-telemetry");
    public static bool IsNoUpdates(this ParseResult result) => result.ValueForOption<bool>("--no-updates");

    public static string? GetWorkspaceId(this ParseResult result) => result.ValueForOption<string>("--workspace");
    public static bool? GetLiveMode(this ParseResult result) => result.ValueForOption<bool?>("--live");

    public static bool TryGetWorkspaceId(this ParseResult result, [NotNullWhen(true)] out string? workspaceId)
    {
        workspaceId = result.GetWorkspaceId();
        return !string.IsNullOrWhiteSpace(workspaceId);
    }

    public static bool TryGetLiveMode(this ParseResult result, [NotNullWhen(true)] out bool? liveMode)
    {
        liveMode = result.GetLiveMode();
        return liveMode is not null;
    }
}
