namespace Falu.Commands.Money.Statements;

internal class MoneyStatementsCommand : Command
{
    public MoneyStatementsCommand() : base("statements", "Work with money statements.")
    {
        Add(new MoneyStatementsListCommand());
        //new MoneyStatementsGetCommand(),
        Add(new MoneyStatementsUploadCommand());
    }
}
