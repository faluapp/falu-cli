using Spectre.Console;

namespace Falu.Commands.Money.Balances;

internal class MoneyBalancesGetCommand() : WorkspacedCommand("get", "Get money balances")
{
    public override async Task<int> ExecuteAsync(CliCommandExecutionContext context, CancellationToken cancellationToken)
    {
        var response = await context.Client.MoneyBalances.GetAsync(cancellationToken: cancellationToken);
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
            table.AddRow(new Markup("MPESA"), new Markup(code), new Markup($"KES {balance / 100f:n2}"), new Markup(updated).Centered());
        }

        AnsiConsole.Write(table);

        return 0;
    }
}
