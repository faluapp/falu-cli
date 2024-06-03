namespace Falu.Commands.RequestLogs;

internal class RequestLogsCommand : CliCommand
{
    public RequestLogsCommand() : base("logs", "Work with request logs.")
    {
        Add(new RequestLogsTailCommand());
    }
}
