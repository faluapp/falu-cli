using Falu.Client;
using Falu.Commands;
using Falu.Config;
using Spectre.Console;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Text;
using Res = Falu.Properties.Resources;

namespace Falu;

internal class FaluRootCliAction(ConfigValuesLoader configValuesLoader, ConfigValues configValues, IHost host) : AsynchronousCliAction
{
    private static readonly System.Reflection.AssemblyName AssemblyName = typeof(CliCommand).Assembly.GetName();
    private static readonly ActivitySource ActivitySource = new(AssemblyName.Name!, AssemblyName.Version!.ToString());

    public override async Task<int> InvokeAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        var command = (FaluExecutableCliCommand)parseResult.CommandResult.Command;

        // create a scoped service provider
        await using var scope = host.Services.CreateAsyncScope();
        var provider = scope.ServiceProvider;

        // create the execution context
        var environment = provider.GetRequiredService<IHostEnvironment>();
        var client = provider.GetRequiredService<FaluCliClient>();
        var logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger(command.GetType());
        var context = new CliCommandExecutionContext(provider)
        {
            ConfigValues = configValues,
            ParseResult = parseResult,
            Environment = environment,
            Client = client,
            Logger = logger,
        };

        // start the host
        await host.StartAsync(cancellationToken);

        // execute the command with tracking and post actions
        try
        {
            return await TrackedAsync(context, command, cancellationToken);
        }
        catch (Exception ex) { return HandleException(ex); }
        finally
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                // check for updates must be done last because it should only run after what the user requested
                await CheckUpdatesAsync(command, context, cancellationToken);

                // save the configuration values
                if (await configValuesLoader.SaveAsync(configValues, cancellationToken))
                {
                    context.Logger.LogTrace("Updated configuration values saved.");
                }
            }

            // stop the host, this will stop and dispose the services which flushes OpenTelemetry data
            await host.StopAsync(cancellationToken);
        }
    }

    private static async Task<int> TrackedAsync(CliCommandExecutionContext context, FaluExecutableCliCommand command, CancellationToken cancellationToken)
    {
        // inspired by https://medium.com/@asimmon/instrumenting-system-commandline-based-net-applications-6d910f91b8a8

        static string GetFullCommandName(ParseResult parseResult)
        {
            var names = new List<string>();
            var result = parseResult.CommandResult;

            while (result != null && result != parseResult.RootCommandResult)
            {
                names.Add(result.Command.Name);
                result = result.Parent as CommandResult;
            }

            names.Reverse();

            return string.Join(' ', names);
        }

        static string Redact(string value) => Constants.ApiKeyFormat.IsMatch(value) ? "***REDACTED***" : value;

        var activity = ActivitySource.StartActivity("Command", ActivityKind.Consumer);
        if (activity is null)
        {
            return await command.ExecuteAsync(context, cancellationToken);
        }

        // track command name and arguments
        var commandName = GetFullCommandName(context.ParseResult);
        var commandArgs = string.Join(' ', context.ParseResult.Tokens.Select(t => Redact(t.Value)));
        activity.DisplayName = commandName;
        activity.SetTag("command.name", commandName);
        activity.SetTag("command.args", commandArgs);

        // track the workspace identifier
        var configValues = context.ConfigValues;
        if (command.TryGetWorkspace(context.ParseResult, out var workspaceId) && !configValues.TryGetWorkspaceId(workspaceId, out workspaceId)) workspaceId = null;
        workspaceId ??= configValues.DefaultWorkspaceId;
        if (workspaceId is not null) activity.SetTag("workspace.id", workspaceId);

        // track the live mode
        var live = command.GetLiveMode(context.ParseResult) ?? configValues.DefaultLiveMode;
        if (live is not null) activity.SetTag("live_mode", live.Value.ToString().ToLowerInvariant());

        try
        {
            var result = await command.ExecuteAsync(context, cancellationToken);

            activity.SetStatus(ActivityStatusCode.Ok);
            activity.Stop();
            return result;
        }
        catch (Exception ex)
        {
            var cancelled = cancellationToken.IsCancellationRequested;

            if (!cancelled && activity.IsAllDataRequested)
            {
                activity.AddTag("exception.type", ex.GetType().FullName);
                activity.AddTag("exception.message", ex.Message);
                activity.AddTag("exception.stacktrace", ex.StackTrace);
            }

            activity.SetStatus(cancelled ? ActivityStatusCode.Ok : ActivityStatusCode.Error);
            activity.Stop();

            throw;
        }
    }
    private static int HandleException(Exception exception)
    {
        if (exception is OperationCanceledException) { } // nothing to do
        else if (exception is FaluException fe)
        {
            var error = fe.Error;
            if (error is not null)
            {
                var sb = new StringBuilder();

                sb.AppendLine(SpectreFormatter.ColouredRed(Res.RequestFailedHeader));

                var id = fe.RequestId;
                if (!string.IsNullOrEmpty(id))
                {
                    sb.AppendLine(SpectreFormatter.ColouredRed(string.Format(Res.RequestIdFormat, id)));
                }

                id = fe.TraceId;
                if (!string.IsNullOrEmpty(id))
                {
                    sb.AppendLine(SpectreFormatter.ColouredRed(string.Format(Res.TraceIdentifierFormat, id)));
                }

                sb.AppendLine(SpectreFormatter.ColouredRed(string.Format(Res.ProblemDetailsErrorCodeFormat, error.Title)));
                if (!string.IsNullOrWhiteSpace(error.Detail))
                {
                    sb.AppendLine(SpectreFormatter.ColouredRed(string.Format(Res.ProblemDetailsErrorDetailFormat, error.Detail)));
                }

                if (error.Errors is not null && error.Errors.Count > 0)
                {
                    var errors = string.Join(Environment.NewLine, error.Errors.Select(k => $"{k.Key}: {string.Join("; ", k.Value)}"));
                    sb.AppendLine(SpectreFormatter.ColouredRed(string.Format(Res.ProblemDetailsErrorsFormat, errors)));
                }

                AnsiConsole.MarkupLine(sb.ToString());
            }
            else if (fe.StatusCode == HttpStatusCode.Unauthorized)
            {
                AnsiConsole.MarkupLine(SpectreFormatter.ColouredRed(Res.Unauthorized401ErrorMessage));
            }
            else if (fe.StatusCode == HttpStatusCode.Forbidden)
            {
                AnsiConsole.MarkupLine(SpectreFormatter.ColouredRed(Res.Forbidden403Message));
            }
            else if (fe.StatusCode == HttpStatusCode.InternalServerError)
            {
                AnsiConsole.MarkupLine(SpectreFormatter.ColouredRed(Res.InternalServerError500Message));
            }
            else
            {
                AnsiConsole.MarkupLine(SpectreFormatter.ColouredRed(fe.Message));
            }
        }
        else if (exception is LoginException le)
        {
            AnsiConsole.MarkupLine(
                SpectreFormatter.ColouredRed(
                    string.Format(
                        le.InnerException is null ? Res.LoginFailedWithCodeFormat : Res.LoginFailedFormat,
                        le.Message)));
        }
        else if (exception is HttpRequestException hre && hre.InnerException is SocketException se && se.SocketErrorCode == SocketError.HostNotFound)
        {
            AnsiConsole.MarkupLine(SpectreFormatter.ColouredRed(Res.HostNotFoundExceptionFormat));
        }
        else
        {
            var format = ExceptionFormats.ShortenPaths
                       | ExceptionFormats.ShortenTypes
                       | ExceptionFormats.ShortenMethods
                       | ExceptionFormats.ShowLinks;
            AnsiConsole.WriteException(exception, format);
        }

        return 1;
    }
    private static async Task CheckUpdatesAsync(FaluExecutableCliCommand command, CliCommandExecutionContext context, CancellationToken cancellationToken)
    {
        // At this point, we can check if a newer version was found.
        // This code will not be reached if there's an exception but validation errors do get here.

        // disable update checks if
        // - in development
        // - debugging
        // - the command has disabled updates
        // - the configuration has disabled it
        // - the last update check was less than 24 hours ago
        var configValues = context.ConfigValues;
        var disabled = context.Environment.IsDevelopment()
                       || Debugger.IsAttached
                       || command.IsNoUpdates(context.ParseResult)
                       || configValues.NoUpdates
                       || configValues.LastUpdateCheck > DateTimeOffset.UtcNow.AddHours(-24);

        if (disabled)
        {
            context.Logger.LogTrace("Update checks are disabled. Skipping ...");
        }
        else
        {
            var client = context.GetRequiredService<IHttpClientFactory>().CreateClient("Updates");
            GitHubLatestRelease? release = null;
            try
            {
                const string url = $"https://api.github.com/repos/{Constants.RepositoryOwner}/{Constants.RepositoryName}/releases/latest";
                context.Logger.LogTrace("Fetching latest version from {Url}", url);
                release = await client.GetFromJsonAsync(url, FaluCliJsonSerializerContext.Default.GitHubLatestRelease, cancellationToken);
            }
            catch (Exception ex)
            {
                context.Logger.LogTrace(ex, "Failed to fetch latest version");
                // nothing more to do here, updates are not crucial
            }

            if (release is not null)
            {
                var current = SemanticVersioning.Version.Parse(VersioningHelper.ProductVersion);
                if (SemanticVersioning.Version.TryParse(release.TagName, out var latest) && latest > current)
                {
                    var sb = new StringBuilder();

                    sb.AppendLine(); // empty line

                    sb.Append("New version (");
                    sb.Append(SpectreFormatter.ColouredLightGreen($"{latest}"));
                    sb.AppendLine($") is available. You have version {current.BaseVersion()}");

                    sb.Append("Download at: ");
                    sb.AppendLine(SpectreFormatter.ColouredLightGreen(release.HtmlUrl!));

                    AnsiConsole.MarkupLine(sb.ToString());
                    AnsiConsole.WriteLine(); // empty line
                }

                // update the last check time
                configValues.LastUpdateCheck = DateTimeOffset.UtcNow;
            }
        }
    }
}
