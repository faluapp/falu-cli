using System.Text.Json.Serialization;

namespace Falu.Client.MoneyStatements;

public class MoneyStatementUploadResponse : MoneyStatement
{
    /// <summary>
    /// Records extracted from the statement.
    /// </summary>
    [JsonPropertyName("extracted")]
    public List<ExtractedStatementRecord> Extracted { get; set; } = [];
}

public record ExtractedStatementRecord
{
    [JsonPropertyName("mpesa")]
    public ExtractedMpesaStatementRecord? Mpesa { get; set; }
}

public record ExtractedMpesaStatementRecord
{
    [JsonPropertyName("receipt")]
    public string? Receipt { get; set; }
}
