namespace Falu.Commands.Money.Statements;

public class MoneyStatementsListCommand : Command
{
    public MoneyStatementsListCommand() : base("list", "List recent money statements")
    {
        this.AddOption<string[]>(["--object-kind"],
                                 description: "The object type to filter statements for.",
                                 configure: o => o.FromAmong("payments", "payment_refunds", "transfers", "transfer_reversals"));

        this.AddOption<string[]>(["--provider"],
                                 description: "Type of provider to filter statements for.",
                                 configure: o => o.FromAmong("mpesa"));

        this.AddOption<bool?>(["--uploaded"],
                              description: "Whether to only list uploaded statements");

        this.AddOption(["--count"],
                       description: "Number of records to retrieve",
                       defaultValue: 10);
    }
}
