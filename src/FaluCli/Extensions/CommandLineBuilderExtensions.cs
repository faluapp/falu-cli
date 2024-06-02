using Falu;
using Falu.Commands.Login;
using Falu.Config;
using Spectre.Console;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Text;
using Res = Falu.Properties.Resources;

namespace System.CommandLine.Builder;

/// <summary>
/// Extensions for <see cref="CommandLineBuilder"/>
/// </summary>
internal static class CommandLineBuilderExtensions
{
    private static readonly Reflection.AssemblyName AssemblyName = typeof(CommandLineBuilder).Assembly.GetName();
    private static readonly ActivitySource ActivitySource = new(AssemblyName.Name!, AssemblyName.Version!.ToString());

    public static CommandLineBuilder UseFaluDefaults(this CommandLineBuilder builder, ConfigValues configValues)
    {
        ArgumentNullException.ThrowIfNull(builder, nameof(builder));

        return builder.UseActivity()
                      .UseVersionOption()
                      .UseHelp()
                      .UseEnvironmentVariableDirective()
                      .UseParseDirective()
                      .UseSuggestDirective()
                      .RegisterWithDotnetSuggest()
                      .UseTypoCorrections()
                      .UseParseErrorReporting()
                      .UseExceptionHandler(ExceptionHandler)
                      .CancelOnProcessTermination()
                      .UseUpdateChecker(configValues) /* update checker middleware must be added last because it should only run after what the user requested */;
    }

    private static CommandLineBuilder UseActivity(this CommandLineBuilder builder)
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

        return builder.AddMiddleware(async (context, next) =>
        {
            var activity = ActivitySource.StartActivity("Command", ActivityKind.Consumer);
            if (activity is null)
            {
                await next(context);
                return;
            }

            // Track command name, command arguments and username
            var commandName = GetFullCommandName(context.ParseResult);
            var commandArgs = string.Join(' ', context.ParseResult.Tokens.Select(t => Redact(t.Value)));
            activity.DisplayName = commandName;
            activity.SetTag("command.name", commandName);
            activity.SetTag("command.args", commandArgs);
            if (context.TryGetWorkspaceId(out var workspaceId)) activity.SetTag("workspace.id", workspaceId);
            if (context.TryGetLiveMode(out var live)) activity.SetTag("live_mode", live.ToString());

            try
            {
                await next(context);

                activity.SetStatus(ActivityStatusCode.Ok);
                activity.Stop();
            }
            catch (Exception ex)
            {
                var cancelled = context.GetCancellationToken().IsCancellationRequested;

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
        }, MiddlewareOrder.Default); // default = 0, anything less than that and the activity will be null because the host (which adds open telemetry) is registered at default too
    }

    private static void ExceptionHandler(Exception exception, InvocationContext context)
    {
        context.ExitCode = 1;

        if (exception is OperationCanceledException) return;

        if (exception is FaluException fe)
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
    }

    private static CommandLineBuilder UseUpdateChecker(this CommandLineBuilder builder, ConfigValues configValues)
    {
        return builder.AddMiddleware(async (invocation, next) =>
        {
            try
            {
                await next(invocation);
            }
            finally
            {
                // At this point, we can check if a newer version was found.
                // This code will not be reached if there's an exception but validation errors do get here.

                // disable update checks if
                // - in development
                // - debugging
                // - the command has disabled updates
                // - the configuration has disabled it
                // - the last update check was less than 24 hours ago
                var provider = invocation.BindingContext.GetRequiredService<IHost>().Services;
                var logger = provider.GetRequiredService<ILoggerProvider>().CreateLogger("Updates");
                var environment = provider.GetRequiredService<IHostEnvironment>();
                var disabled = environment.IsDevelopment()
                               || Debugger.IsAttached
                               || invocation.IsNoUpdates()
                               || configValues.NoUpdates
                               || configValues.LastUpdateCheck > DateTimeOffset.UtcNow.AddHours(-24);

                if (disabled)
                {
                    logger.LogTrace("Update checks are disabled");
                }
                else
                {
                    var cancellationToken = invocation.GetCancellationToken();
                    var client = provider.GetRequiredService<IHttpClientFactory>().CreateClient("Updates");
                    GitHubLatestRelease? release = null;
                    try
                    {
                        const string url = $"https://api.github.com/repos/{Constants.RepositoryOwner}/{Constants.RepositoryName}/releases/latest";
                        logger.LogTrace("Fetching latest version from {Url}", url);
                        release = await client.GetFromJsonAsync(url, FaluCliJsonSerializerContext.Default.GitHubLatestRelease, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        logger.LogTrace(ex, "Failed to fetch latest version");
                        // nothing more to do here, updates are not crucial
                    }

                    if (release is not null)
                    {
                        var current = VersioningHelper.CurrentVersion;
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
                        var configValuesProvider = provider.GetRequiredService<IConfigValuesProvider>();
                        await configValuesProvider.SaveConfigValuesAsync(cancellationToken);
                    }
                }
            }
        });
    }
}
