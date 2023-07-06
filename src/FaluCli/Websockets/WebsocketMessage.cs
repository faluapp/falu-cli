using System.Text.Json.Serialization;

namespace Falu.Websockets;

internal class WebsocketMessage
{
    [JsonPropertyName("type")]
    public string? Type { get; set; }
}
