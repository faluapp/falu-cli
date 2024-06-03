using Falu.Config;
using System.Diagnostics.CodeAnalysis;

namespace System.CommandLine;

/// <summary>Extensions for <see cref="InvocationContext"/>.</summary>
internal static class InvocationContextExtensions
{
    public static bool IsVerboseEnabled(this InvocationContext context) => context.ParseResult.ValueForOption<bool>("--verbose");
    public static bool IsNoTelemetry(this InvocationContext context) => context.ParseResult.ValueForOption<bool>("--no-telemetry");
    public static bool IsNoUpdates(this InvocationContext context) => context.ParseResult.ValueForOption<bool>("--no-updates");

    public static string? GetWorkspaceId(this InvocationContext context) => context.ParseResult.ValueForOption<string>("--workspace");
    public static bool? GetLiveMode(this InvocationContext context) => context.ParseResult.ValueForOption<bool?>("--live");

    public static bool TryGetWorkspaceId(this InvocationContext context, [NotNullWhen(true)] out string? workspaceId)
    {
        workspaceId = context.GetWorkspaceId();
        return !string.IsNullOrWhiteSpace(workspaceId);
    }

    public static bool TryGetLiveMode(this InvocationContext context, [NotNullWhen(true)] out bool? liveMode)
    {
        liveMode = context.GetLiveMode();
        return liveMode is not null;
    }

    public static ConfigValuesLoader GetConfigValuesLoader(this InvocationContext context) => context.BindingContext.GetRequiredService<ConfigValuesLoader>();
    public static ConfigValues GetConfigValues(this InvocationContext context) => context.BindingContext.GetRequiredService<ConfigValues>();

    public static T? GetService<T>(this InvocationContext context)
        => context.BindingContext.GetService<T>() ?? context.BindingContext.GetRequiredService<IHost>().Services.GetService<T>();
    public static T GetRequiredService<T>(this InvocationContext context) where T : notnull
        => context.BindingContext.GetService<T>() ?? context.BindingContext.GetRequiredService<IHost>().Services.GetRequiredService<T>();
}
