using Falu.Client;
using Falu.Config;
using System.Diagnostics.CodeAnalysis;
using Res = Falu.Properties.Resources;

namespace Falu;

internal sealed class CliCommandExecutionContext(IServiceProvider serviceProvider)
{
    public required ConfigValues ConfigValues { get; init; }

    public required ParseResult ParseResult { get; init; }

    public required IHostEnvironment Environment { get; init; }
    public required FaluCliClient Client { get; init; }
    public required ILogger Logger { get; init; }

    /// <summary>
    /// Get service of type <typeparamref name="T"/> from the scoped <see cref="IServiceProvider"/>.
    /// </summary>
    /// <typeparam name="T">The type of service object to get.</typeparam>
    /// <returns>A service object of type <typeparamref name="T"/>.</returns>
    /// <exception cref="InvalidOperationException">There is no service of type <typeparamref name="T"/>.</exception>
    public T GetRequiredService<T>() where T : notnull => serviceProvider.GetRequiredService<T>();
}

internal abstract class FaluCliCommand : CliCommand
{
    private readonly CliOption<bool> verboseOption;
    private readonly CliOption<bool> noTelemetryOption;
    private readonly CliOption<bool> noUpdatesOption;
    private readonly CliOption<string>? apiKeyOption;
    private readonly CliOption<string>? workspaceOption;
    private readonly CliOption<bool?>? liveOption;
    private readonly CliOption<string>? idempotencyKeyOption;

    public FaluCliCommand(string name, string? description = null, bool workspaced = false) : base(name, description)
    {
        verboseOption = new CliOption<bool>(name: "--verbose", aliases: ["-v"]) { Description = "Whether to output verbosely.", DefaultValueFactory = r => false, };
        Add(verboseOption);

        noTelemetryOption = new CliOption<bool>(name: "--no-telemetry") { Description = Res.OptionDescriptionNoTelemetry, DefaultValueFactory = r => false, };
        Add(noTelemetryOption);

        noUpdatesOption = new CliOption<bool>(name: "--no-updates") { Description = Res.OptionDescriptionNoUpdates, DefaultValueFactory = r => false, };
        Add(noUpdatesOption);

        if (Workspaced = workspaced)
        {
            apiKeyOption = new CliOption<string>(name: "--apikey") { Description = Res.OptionDescriptionApiKey, };
            apiKeyOption.MatchesFormat(Constants.ApiKeyFormat);
            Add(apiKeyOption);

            workspaceOption = new CliOption<string>(name: "--workspace") { Description = Res.OptionDescriptionWorkspace, };
            // can't validate because we do not have access to the ConfigValues here but the FaluCliClientHandler will validate it
            Add(workspaceOption);

            liveOption = new CliOption<bool?>(name: "--live") { Description = Res.OptionDescriptionLive, };
            Add(liveOption);

            idempotencyKeyOption = new CliOption<string>(name: "--idempotency-key") { Description = Res.OptionDescriptionIdempotencyKey, };
            idempotencyKeyOption.MatchesFormat(Constants.IdempotencyKeyFormat);
            Add(idempotencyKeyOption);
        }
    }

    public bool IsVerboseEnabled(ParseResult result) => result.GetValue(verboseOption);
    public bool IsNoTelemetry(ParseResult result) => result.GetValue(noTelemetryOption);
    public bool IsNoUpdates(ParseResult result) => result.GetValue(noUpdatesOption);

    [MemberNotNullWhen(true, nameof(apiKeyOption))]
    [MemberNotNullWhen(true, nameof(workspaceOption))]
    [MemberNotNullWhen(true, nameof(liveOption))]
    [MemberNotNullWhen(true, nameof(idempotencyKeyOption))]
    protected virtual bool Workspaced { get; }

    public string? GetApiKey(ParseResult result) => Workspaced ? result.GetValue(apiKeyOption) : null;
    public string? GetWorkspace(ParseResult result) => Workspaced ? result.GetValue(workspaceOption) : null;
    public bool? GetLiveMode(ParseResult result) => Workspaced ? result.GetValue(liveOption) : null;
    public string? GetIdempotencyKey(ParseResult result) => Workspaced ? result.GetValue(idempotencyKeyOption) : null;

    public bool TryGetWorkspace(ParseResult result, [NotNullWhen(true)] out string? workspaceId) => !string.IsNullOrWhiteSpace(workspaceId = GetWorkspace(result));
}

internal abstract class FaluExecuteableCliCommand(string name, string? description = null, bool workspaced = false) : FaluCliCommand(name, description, workspaced)
{
    /// <inheritdoc/>
    public abstract Task<int> ExecuteAsync(CliCommandExecutionContext context, CancellationToken cancellationToken);
}

// TODO: consider replacing this with FaluExecuteableCliCommand and overriding Workspaced property in the specific command
internal abstract class WorkspacedCommand(string name, string? description = null) : FaluExecuteableCliCommand(name, description, workspaced: true) { }
