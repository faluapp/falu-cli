using System.Net;
using System.Text.Json.Serialization;

namespace Falu.Websockets;

public class RealtimeConnectionFilters
{
    [JsonPropertyName("logs")]
    public RealtimeConnectionFilterLogs? Logs { get; set; }

    [JsonPropertyName("events")]
    public RealtimeConnectionFilterEvents? Events { get; set; }
}

public class RealtimeConnectionFilterLogs
{
    [JsonPropertyName("ip_addresses")]
    public IEnumerable<IPAddress>? IPAddresses { get; set; }

    [JsonPropertyName("paths")]
    public IEnumerable<string>? Paths { get; set; }

    [JsonPropertyName("methods")]
    public IEnumerable<string>? Methods { get; set; }

    [JsonPropertyName("status_codes")]
    public IEnumerable<int>? StatusCodes { get; set; }

    [JsonPropertyName("sources")]
    public IEnumerable<string>? Sources { get; set; }
}

public class RealtimeConnectionFilterEvents
{
    [JsonPropertyName("types")]
    public IEnumerable<string>? Types { get; set; }
}
