﻿using Falu.Config;

namespace Falu.Commands.Login;

internal class LogoutCommandHandler : ICommandHandler
{
    private readonly IConfigValuesProvider configValuesProvider;
    private readonly ILogger logger;

    public LogoutCommandHandler(IConfigValuesProvider configValuesProvider, ILogger<LogoutCommandHandler> logger)
    {
        this.configValuesProvider = configValuesProvider ?? throw new ArgumentNullException(nameof(configValuesProvider));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    int ICommandHandler.Invoke(InvocationContext context) => throw new NotImplementedException();

    public async Task<int> InvokeAsync(InvocationContext context)
    {
        var cancellationToken = context.GetCancellationToken();

        // clear the authentication information and save
        var values = await configValuesProvider.GetConfigValuesAsync(cancellationToken);
        values.Authentication = null;
        await configValuesProvider.SaveConfigValuesAsync(cancellationToken);
        logger.LogInformation("Authentication information cleared.");

        return 0;
    }
}
