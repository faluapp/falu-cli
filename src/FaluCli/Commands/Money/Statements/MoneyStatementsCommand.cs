namespace Falu.Commands.Money.Statements;

internal class MoneyStatementsCommand : CliCommand
{
    public MoneyStatementsCommand() : base("statements", "Work with money statements.")
    {
        Add(new MoneyStatementsListCommand());
        //new MoneyStatementsGetCommand(),
        Add(new MoneyStatementsUploadCommand());
    }
}
