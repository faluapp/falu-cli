namespace Falu.Commands.RequestLogs;

internal class RequestLogsCommand : FaluCliCommand
{
    public RequestLogsCommand() : base("logs", "Work with request logs.")
    {
        Add(new RequestLogsTailCommand());
    }
}
