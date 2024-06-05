namespace Falu.Commands.Money.Statements;

internal class MoneyStatementsCommand : FaluCliCommand
{
    public MoneyStatementsCommand() : base("statements", "Work with money statements.")
    {
        Add(new MoneyStatementsListCommand());
        //new MoneyStatementsGetCommand(),
        Add(new MoneyStatementsUploadCommand());
    }
}
