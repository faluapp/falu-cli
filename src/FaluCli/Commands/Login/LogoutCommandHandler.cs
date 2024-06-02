namespace Falu.Commands.Login;

internal class LogoutCommandHandler(ILogger<LogoutCommandHandler> logger) : ICommandHandler
{
    int ICommandHandler.Invoke(InvocationContext context) => throw new NotImplementedException();

    public Task<int> InvokeAsync(InvocationContext context)
    {
        // clear the authentication information and save
        var values = context.GetConfigValues();
        values.Authentication = null;
        logger.LogInformation("Authentication information cleared.");

        return Task.FromResult(0);
    }
}
