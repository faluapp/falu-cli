using Falu.Payments;

namespace Falu.Commands.Money.Balances;

internal class MoneyBalancesRefreshCommand : WorkspacedCommand
{
    public MoneyBalancesRefreshCommand() : base("refresh", "Request refresh of money balances") { }

    public override async Task<int> ExecuteAsync(CliCommandExecutionContext context, CancellationToken cancellationToken)
    {
        var request = new MoneyBalancesRefreshRequest { };
        var response = await context.Client.MoneyBalances.RefreshAsync(request, cancellationToken: cancellationToken);
        response.EnsureSuccess();

        context.Logger.LogInformation("Refresh requested! You can check back later using 'falu money-balances get'");

        return 0;
    }
}
