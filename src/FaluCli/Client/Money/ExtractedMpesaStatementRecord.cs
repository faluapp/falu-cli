using System.Text.Json.Serialization;

namespace Falu.Client;

public record ExtractedMpesaStatementRecord
{
    [JsonPropertyName("receipt")]
    public string? Receipt { get; set; }
}

public record ExtractedStatementRecord
{
    [JsonPropertyName("mpesa")]
    public ExtractedMpesaStatementRecord? Mpesa { get; set; }
}
