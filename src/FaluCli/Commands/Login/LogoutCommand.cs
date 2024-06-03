namespace Falu.Commands.Login;

internal class LogoutCommand : FaluCliCommand
{
    public LogoutCommand() : base("logout", "Logout of your Falu account from the CLI") { }

    public override Task<int> ExecuteAsync(CliCommandExecutionContext context, CancellationToken cancellationToken)
    {
        context.ConfigValues.Authentication = null;
        context.ConfigValues.Workspaces = [];
        context.Logger.LogInformation("Authentication information cleared.");

        return Task.FromResult(0);
    }
}
