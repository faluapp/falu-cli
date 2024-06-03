using Falu.Commands.Money.Balances;
using Falu.Commands.Money.Statements;

namespace Falu.Commands.Money;

internal class MoneyCommand : WorkspacedCommand
{
    public MoneyCommand() : base("money", "Work with money.")
    {
        Add(new MoneyBalancesCommand());
        Add(new MoneyStatementsCommand());
    }
}