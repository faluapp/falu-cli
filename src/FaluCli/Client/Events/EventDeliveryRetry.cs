using System.Text.Json.Serialization;

namespace Falu.Client.Events;

public class EventDeliveryRetry
{
    [JsonPropertyName("webhook_endpoint")]
    public string? WebhookEndpoint { get; set; }
}
