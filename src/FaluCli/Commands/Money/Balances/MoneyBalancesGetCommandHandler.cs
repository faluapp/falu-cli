using Falu.Client;
using Spectre.Console;

namespace Falu.Commands.Money.Balances;

internal class MoneyBalancesGetCommandHandler(FaluCliClient client) : ICommandHandler
{
    int ICommandHandler.Invoke(InvocationContext context) => throw new NotImplementedException();

    public async Task<int> InvokeAsync(InvocationContext context)
    {
        var cancellationToken = context.GetCancellationToken();

        var response = await client.MoneyBalances.GetAsync(cancellationToken: cancellationToken);
        response.EnsureSuccess();

        var balances = response.Resource!;

        // Create a table
        var table = new Table().AddColumn("Type")
                               .AddColumn("Code")
                               .AddColumn(new TableColumn("Balance").RightAligned())
                               .AddColumn(new TableColumn("Updated").Centered());

        // Add rows
        var updated = balances.Updated.ToLocalTime().ToString("F");
        foreach (var (code, balance) in balances.Mpesa ?? new())
        {
            table.AddRow(new Markup("MPESA"),
                         new Markup(code),
                         new Markup($"KES {balance / 100f:n2}"),
                         new Markup(updated).Centered());
        }

        AnsiConsole.Write(table);

        return 0;
    }
}
