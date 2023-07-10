namespace Falu.Client.MoneyStatements;

public class MoneyStatementUploadResponse : MoneyStatement
{
    /// <summary>
    /// Records extracted from the statement.
    /// </summary>
    public List<ExtractedStatementRecord> Extracted { get; set; } = new();
}
