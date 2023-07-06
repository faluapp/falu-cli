using System.Text.Json.Serialization;

namespace Falu.Client.Realtime;

public class RealtimeConnectionNegotiationRequest
{
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("purpose")]
    public string? Purpose { get; set; }

    [JsonPropertyName("ttl")]
    public string Ttl { get; set; } = "PT1H";
}
