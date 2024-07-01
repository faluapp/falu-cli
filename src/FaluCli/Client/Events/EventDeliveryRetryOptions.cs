using System.Text.Json.Serialization;

namespace Falu.Client.Events;

public class EventDeliveryRetryOptions
{
    [JsonPropertyName("webhook_endpoint")]
    public string? WebhookEndpoint { get; set; }
}
