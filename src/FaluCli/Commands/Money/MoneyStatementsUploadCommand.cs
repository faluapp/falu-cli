namespace Falu.Commands.Money;

public class MoneyStatementsUploadCommand : Command
{
    public MoneyStatementsUploadCommand() : base("upload", "Upload a statement to Falu to resolve pending payments, transfers, refunds, or reversals for bring-your-own providers.")
    {
        this.AddArgument<string>(name: "object-kind",
                                 description: "The object type to upload the statement against.",
                                 configure: o => o.FromAmong("payments", "payment_refunds", "transfers", "transfer_reversals"));

        this.AddOption<string>(new[] { "-f", "--file", },
                               description: $"File path for the statement file (up to {Constants.MaxStatementFileSizeString}).",
                               configure: o => o.IsRequired = true);

        this.AddOption(new[] { "--provider", },
                       description: "Type of statement",
                       defaultValue: "mpesa",
                       configure: o => o.FromAmong("mpesa"));
    }
}
