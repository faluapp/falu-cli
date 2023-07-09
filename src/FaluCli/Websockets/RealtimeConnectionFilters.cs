using System.Net;
using System.Text.Json.Serialization;

namespace Falu.Websockets;

public class RealtimeConnectionFilters
{
    [JsonPropertyName("logs")]
    public RealtimeConnectionFilterLogs? Logs { get; set; }

    [JsonPropertyName("events")]
    public RealtimeConnectionFilterEvents? Events { get; set; }

    public RealtimeConnectionFilters? NullIfEmpty()
    {
        var objects = new object?[] { Logs, Events, };
        return objects.Any(o => o is null) ? null : this;
    }
}

public class RealtimeConnectionFilterLogs
{
    [JsonPropertyName("ip_addresses")]
    public IPAddress[]? IPAddresses { get; set; }

    [JsonPropertyName("paths")]
    public string[]? Paths { get; set; }

    [JsonPropertyName("methods")]
    public string[]? Methods { get; set; }

    [JsonPropertyName("status_codes")]
    public int[]? StatusCodes { get; set; }

    [JsonPropertyName("sources")]
    public string[]? Sources { get; set; }

    public RealtimeConnectionFilterLogs? NullIfEmpty()
    {
        var objects = new object?[] { IPAddresses, Paths, Methods, StatusCodes, Sources, };
        return objects.Any(o => o is null) ? null : this;
    }
}

public class RealtimeConnectionFilterEvents
{
    [JsonPropertyName("types")]
    public string[]? Types { get; set; }

    public RealtimeConnectionFilterEvents? NullIfEmpty()
    {
        var objects = new object?[] { Types, };
        return objects.Any(o => o is null) ? null : this;
    }
}
