using System.Text.Json.Serialization;

namespace Falu.Client.MoneyStatements;

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
