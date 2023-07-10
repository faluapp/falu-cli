namespace Falu.Commands.Money.Balances;

public class MoneyStatementsListCommand : Command
{
    public MoneyStatementsListCommand() : base("list", "List recent money statements")
    {
        this.AddOption<string[]>(new[] { "--object-kind", },
                                 description: "The object type to filter statements for.",
                                 configure: o => o.FromAmong("payments", "payment_refunds", "transfers", "transfer_reversals"));

        this.AddOption<string[]>(new[] { "--provider", },
                                 description: "Type of provider to filter statements for.",
                                 configure: o => o.FromAmong("mpesa"));

        this.AddOption<bool?>(new[] { "--uploaded", },
                              description: "Whether to only list uploaded statements");

        this.AddOption(new[] { "--count", },
                       description: "Number of records to retreive",
                       defaultValue: 10);
    }
}
