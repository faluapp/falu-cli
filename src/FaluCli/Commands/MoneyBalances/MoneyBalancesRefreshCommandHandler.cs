using Falu.Client;
using Falu.Payments;

namespace Falu.Commands.MoneyBalances;

internal class MoneyBalancesRefreshCommandHandler : ICommandHandler
{
    private readonly FaluCliClient client;
    private readonly ILogger logger;

    public MoneyBalancesRefreshCommandHandler(FaluCliClient client, ILogger<MoneyBalancesRefreshCommandHandler> logger)
    {
        this.client = client ?? throw new ArgumentNullException(nameof(client));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    int ICommandHandler.Invoke(InvocationContext context) => throw new NotImplementedException();

    public async Task<int> InvokeAsync(InvocationContext context)
    {
        var cancellationToken = context.GetCancellationToken();

        var request = new MoneyBalancesRefreshRequest { };
        var response = await client.MoneyBalances.RefreshAsync(request, cancellationToken: cancellationToken);
        response.EnsureSuccess();

        logger.LogInformation("Refresh requested! You can check back later using 'falu money-balances get'");

        return 0;
    }
}
