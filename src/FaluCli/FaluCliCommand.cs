using Falu.Client;
using Falu.Commands.Login;
using Falu.Config;
using Spectre.Console;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Text;
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
    public FaluCliCommand(string name, string? description = null) : base(name, description)
    {
        this.AddOption(["-v", "--verbose"], "Whether to output verbosely.", false);
        this.AddOption(["--no-telemetry"], Res.OptionDescriptionNoTelemetry, false);
        this.AddOption(["--no-updates"], Res.OptionDescriptionNoUpdates, false);
    }

    /// <inheritdoc/>
    public abstract Task<int> ExecuteAsync(CliCommandExecutionContext context, CancellationToken cancellationToken);
}

internal class FaluRootCliAction(ConfigValuesLoader configValuesLoader, ConfigValues configValues, HostApplicationBuilder builder) : AsynchronousCliAction
{
    private static readonly System.Reflection.AssemblyName AssemblyName = typeof(CliCommand).Assembly.GetName();
    private static readonly ActivitySource ActivitySource = new(AssemblyName.Name!, AssemblyName.Version!.ToString());

    public override async Task<int> InvokeAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        if (parseResult.CommandResult.Command is not FaluCliCommand command)
        {
            throw new InvalidOperationException("The command is not a FaluCliCommand.");
        }

        // create host and a scoped service provider
        using var host = builder.Build();
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
                await CheckUpdatesAsync(context, cancellationToken);

                // save the configuration values
                await configValuesLoader.SaveAsync(configValues, cancellationToken);
            }

            // stop the host, this will stop and dispose the services which flushes OpenTelemetry data
            await host.StopAsync(cancellationToken);
        }
    }

    private static async Task<int> TrackedAsync(CliCommandExecutionContext context, FaluCliCommand command, CancellationToken cancellationToken)
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

        // Track command name, command arguments and username
        var commandName = GetFullCommandName(context.ParseResult);
        var commandArgs = string.Join(' ', context.ParseResult.Tokens.Select(t => Redact(t.Value)));
        activity.DisplayName = commandName;
        activity.SetTag("command.name", commandName);
        activity.SetTag("command.args", commandArgs);
        if (context.ParseResult.TryGetWorkspaceId(out var workspaceId)) activity.SetTag("workspace.id", workspaceId);
        if (context.ParseResult.TryGetLiveMode(out var live)) activity.SetTag("live_mode", live.ToString());

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
    private static async Task CheckUpdatesAsync(CliCommandExecutionContext context, CancellationToken cancellationToken)
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
                       || context.ParseResult.IsNoUpdates()
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
