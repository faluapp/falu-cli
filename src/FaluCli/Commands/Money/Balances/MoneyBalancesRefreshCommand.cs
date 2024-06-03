using Falu.Client;
using Falu.Payments;

namespace Falu.Commands.Money.Balances;

internal class MoneyBalancesRefreshCommand : Command
{
    public MoneyBalancesRefreshCommand() : base("refresh", "Request refresh of money balances")
    {
        this.SetHandler(HandleAsync);
    }

    private static async Task HandleAsync(InvocationContext context)
    {
        var cancellationToken = context.GetCancellationToken();
        var client = context.GetRequiredService<FaluCliClient>();
        var logger = context.GetRequiredService<ILogger<MoneyBalancesRefreshCommand>>();

        var request = new MoneyBalancesRefreshRequest { };
        var response = await client.MoneyBalances.RefreshAsync(request, cancellationToken: cancellationToken);
        response.EnsureSuccess();

        logger.LogInformation("Refresh requested! You can check back later using 'falu money-balances get'");
    }
}
