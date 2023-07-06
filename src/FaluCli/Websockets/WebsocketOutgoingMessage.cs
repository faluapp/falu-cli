using System.Text.Json.Serialization;

namespace Falu.Websockets;

internal class WebsocketOutgoingMessage : WebsocketMessage
{
    [JsonPropertyName("filters")]
    public RealtimeConnectionFilters? Filters { get; set; }

    [JsonPropertyName("workspace")]
    public string? Workspace { get; set; }

    [JsonPropertyName("live")]
    public bool Live { get; set; }
}
