using Falu.Client.Realtime;
using System.Text.Json.Serialization;

namespace Falu.Websockets;

internal class WebsocketOutgoingMessage : WebsocketMessage
{
    public WebsocketOutgoingMessage() { }

    public WebsocketOutgoingMessage(string type, RealtimeConnectionFilters filters, RealtimeConnectionNegotiation negotiation)
    {
        Type = type ?? throw new ArgumentNullException(nameof(type));
        Filters = filters ?? throw new ArgumentNullException(nameof(filters));

        ArgumentNullException.ThrowIfNull(negotiation);
        Workspace = negotiation.Workspace ?? throw new InvalidOperationException($"{nameof(Workspace)} must be present in the {nameof(negotiation)}");
        Live = negotiation.Live;
    }

    [JsonPropertyName("filters")]
    public RealtimeConnectionFilters? Filters { get; set; }

    [JsonPropertyName("workspace")]
    public string? Workspace { get; set; }

    [JsonPropertyName("live")]
    public bool Live { get; set; }
}
