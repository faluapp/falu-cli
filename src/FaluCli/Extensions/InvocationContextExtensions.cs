namespace System.CommandLine;

/// <summary>
/// Extensions for <see cref="InvocationContext"/>
/// </summary>
internal static class InvocationContextExtensions
{
    public static bool IsVerboseEnabled(this InvocationContext context)
    {
        return context.ParseResult.ValueForOption<bool>("--verbose");
    }

    public static string? GetWorkspaceId(this InvocationContext context)
    {
        return context.ParseResult.ValueForOption<string>("--workspace");
    }

    public static bool? GetLiveMode(this InvocationContext context)
    {
        return context.ParseResult.ValueForOption<bool?>("--live");
    }
}
