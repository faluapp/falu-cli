using System.Net;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Tingle.Extensions.Primitives;

namespace Falu.Client.Realtime;

public class RealtimeNegotiation
{
    [JsonPropertyName("url")]
    public Uri Url { get; set; } = default!;

    [JsonPropertyName("expires")]
    public DateTimeOffset Expires { get; set; }

    [JsonPropertyName("token")]
    public string Token { get; set; } = default!;

    [JsonPropertyName("state")]
    public string State { get; set; } = default!;

    [JsonPropertyName("workspace")]
    public string Workspace { get; set; } = default!;

    [JsonPropertyName("live")]
    public bool Live { get; set; }
}

public abstract class RealtimeNegotiationOptions<TFilters>
{
    [JsonPropertyName("ttl")]
    public Duration? Ttl { get; set; }

    [JsonPropertyName("filters")]
    public TFilters? Filters { get; set; }
}

public class RealtimeNegotiationFiltersRequestLogs
{
    [JsonPropertyName("ip_networks")]
    public IPNetwork[]? IPNetworks { get; set; }

    [JsonPropertyName("ip_addresses")]
    public IPAddress[]? IPAddresses { get; set; }

    [JsonPropertyName("sources")]
    public string[]? Sources { get; set; }

    [JsonPropertyName("paths")]
    public string[]? Paths { get; set; }

    [JsonPropertyName("methods")]
    public string[]? Methods { get; set; }

    [JsonPropertyName("status_codes")]
    public int[]? StatusCodes { get; set; }
}

public class RealtimeNegotiationFiltersEvents
{
    [JsonPropertyName("types")]
    public string[]? Types { get; set; }
}

public class RealtimeNegotiationOptionsEvents
 : RealtimeNegotiationOptions<RealtimeNegotiationFiltersEvents>
{ }

public class RealtimeNegotiationOptionsRequestLogs
 : RealtimeNegotiationOptions<RealtimeNegotiationFiltersRequestLogs>
{ }

internal class RealtimeMessage
{
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("object")]
    public JsonObject? Object { get; set; }
}
