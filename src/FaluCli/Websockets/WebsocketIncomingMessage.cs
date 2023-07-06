using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Falu.Websockets;

internal class WebsocketIncomingMessage : WebsocketMessage
{
    [JsonPropertyName("event")]
    public string? Event { get; set; }

    [JsonPropertyName("object")]
    public JsonObject? Object { get; set; }
}
