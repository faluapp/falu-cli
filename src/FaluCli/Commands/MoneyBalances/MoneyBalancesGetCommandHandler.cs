using Falu.Client;

namespace Falu.Commands.MoneyBalances;

internal class MoneyBalancesGetCommandHandler : ICommandHandler
{
    private readonly FaluCliClient client;
    private readonly ILogger logger;

    public MoneyBalancesGetCommandHandler(FaluCliClient client, ILogger<MoneyBalancesGetCommandHandler> logger)
    {
        this.client = client ?? throw new ArgumentNullException(nameof(client));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    int ICommandHandler.Invoke(InvocationContext context) => throw new NotImplementedException();

    public async Task<int> InvokeAsync(InvocationContext context)
    {
        var cancellationToken = context.GetCancellationToken();

        var response = await client.MoneyBalances.GetAsync(cancellationToken: cancellationToken);
        response.EnsureSuccess();

        var balances = response.Resource!;

        // TODO: use a table here instead

        logger.LogInformation("Balances were last updated at {Updated:F}", balances.Updated.ToLocalTime());
        var mpesa = balances.Mpesa ?? new();
        foreach (var (code, balance) in mpesa)
        {
            logger.LogInformation("Balance for {Code}: KES {Balance:n2}", code, balance / 100f);
        }

        return 0;
    }
}
