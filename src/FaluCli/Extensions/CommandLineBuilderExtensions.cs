using Falu;
using Falu.Commands.Login;
using Falu.Updates;
using Spectre.Console;
using System.CommandLine.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Res = Falu.Properties.Resources;

namespace System.CommandLine.Builder;

/// <summary>
/// Extensions for <see cref="CommandLineBuilder"/>
/// </summary>
internal static class CommandLineBuilderExtensions
{
    public static CommandLineBuilder UseFaluDefaults(this CommandLineBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder, nameof(builder));

        return builder.UseVersionOption()
                      .UseHelp()
                      .UseEnvironmentVariableDirective()
                      .UseParseDirective()
                      .UseSuggestDirective()
                      .RegisterWithDotnetSuggest()
                      .UseTypoCorrections()
                      .UseParseErrorReporting()
                      .UseExceptionHandler(ExceptionHandler)
                      .CancelOnProcessTermination();
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
            AnsiConsole.WriteException(exception);
        }
    }

    public static CommandLineBuilder UseUpdateChecker(this CommandLineBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder, nameof(builder));

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

                var current = UpdateChecker.CurrentVersion;
                var latest = UpdateChecker.LatestVersion;
                if (latest is not null && latest > current)
                {
                    var sb = new StringBuilder();

                    sb.AppendLine(); // empty line

                    sb.Append("New version (");
                    sb.Append(SpectreFormatter.Coloured("lightgreen", $"{latest}"));
                    sb.AppendLine($") is available. You have version {current.BaseVersion()}");

                    sb.Append("Download at: ");
                    sb.AppendLine(SpectreFormatter.Coloured("lightgreen", UpdateChecker.LatestVersionHtmlUrl!));

                    sb.AppendLine(); // empty line
                    sb.Append("Release notes: ");
                    AnsiConsole.MarkupLine(sb.ToString());

                    AnsiConsole.WriteLine(UpdateChecker.LatestVersionBody!);
                    AnsiConsole.WriteLine(); // empty line
                }
            }
        });
    }
}
