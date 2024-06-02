﻿using System.Diagnostics.CodeAnalysis;

namespace System.CommandLine;

/// <summary>
/// Extensions for <see cref="InvocationContext"/>
/// </summary>
internal static class InvocationContextExtensions
{
    public static bool IsVerboseEnabled(this InvocationContext context) => context.ParseResult.ValueForOption<bool>("--verbose");
    public static bool IsNoTelemetry(this InvocationContext context) => context.ParseResult.ValueForOption<bool>("--no-telemetry");
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
}
