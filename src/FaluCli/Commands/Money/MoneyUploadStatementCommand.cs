namespace Falu.Commands.Money;

public class MoneyUploadStatementCommand : Command
{
    private static readonly ICollection<FaluObjectKind> AllowedKinds = new HashSet<FaluObjectKind>
    {
        FaluObjectKind.Payments,
        FaluObjectKind.PaymentRefunds,
        FaluObjectKind.Transfers,
        FaluObjectKind.TransferReversals,
    };

    public MoneyUploadStatementCommand(FaluObjectKind kind)
        : base("mpesa-upload", $"Upload an MPESA statement to Falu for {kind.GetReadableName()}.")
    {
        if (!AllowedKinds.Contains(Kind = kind))
        {
            throw new ArgumentOutOfRangeException(nameof(kind), $"'{nameof(FaluObjectKind)}'.'{kind}' is not allowed.");
        }

        this.AddOption<string>(new[] { "-f", "--file", },
                               description: $"File path for the statement file (up to {Constants.MaxMpesaStatementFileSizeString}).",
                               configure: o => o.IsRequired = true);
    }

    internal FaluObjectKind Kind { get; }
}
