namespace Falu.Commands.RequestLogs;

internal class RequestLogsCommand : WorkspacedCommand
{
    public RequestLogsCommand() : base("logs", "Work with request logs.")
    {
        Add(new RequestLogsTailCommand());
    }
}
