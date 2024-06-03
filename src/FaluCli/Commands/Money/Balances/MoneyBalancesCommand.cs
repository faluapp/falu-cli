namespace Falu.Commands.Money.Balances;

internal class MoneyBalancesCommand : Command
{
    public MoneyBalancesCommand() : base("balances", "Work with money balances.")
    {
        Add(new MoneyBalancesGetCommand());
        Add(new MoneyBalancesRefreshCommand());
    }
}
