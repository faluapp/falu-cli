namespace Falu.Commands.Login;

internal class LogoutCommand : Command
{
    public LogoutCommand() : base("logout", "Logout of your Falu account from the CLI")
    {
        this.SetHandler(Handle);
    }

    private static void Handle(InvocationContext context)
    {
        var logger = context.GetRequiredService<ILogger<LogoutCommand>>();

        // clear the authentication information and save
        var values = context.GetConfigValues();
        values.Authentication = null;
        logger.LogInformation("Authentication information cleared.");
    }
}
