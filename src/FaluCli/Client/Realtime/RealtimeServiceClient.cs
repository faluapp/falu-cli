using Falu.Core;
using System.Net.Http.Json;
using SC = Falu.FaluCliJsonSerializerContext;

namespace Falu.Client.Realtime;

internal class RealtimeServiceClient(HttpClient backChannel, FaluClientOptions options) : BaseServiceClient(backChannel, options)
{
    public virtual Task<ResourceResponse<RealtimeNegotiation>> NegotiateAsync(RealtimeNegotiationOptionsEvents options,
                                                                                        RequestOptions? requestOptions = null,
                                                                                        CancellationToken cancellationToken = default)
    {
        var uri = "/v1/realtime/negotiate/events";
        var content = JsonContent.Create(options, SC.Default.RealtimeNegotiationOptionsEvents);
        return RequestAsync(uri, HttpMethod.Post, SC.Default.RealtimeNegotiation, content, requestOptions, cancellationToken);
    }

    public virtual Task<ResourceResponse<RealtimeNegotiation>> NegotiateAsync(RealtimeNegotiationOptionsRequestLogs options,
                                                                                        RequestOptions? requestOptions = null,
                                                                                        CancellationToken cancellationToken = default)
    {
        var uri = "/v1/realtime/negotiate/request_logs";
        var content = JsonContent.Create(options, SC.Default.RealtimeNegotiationOptionsRequestLogs);
        return RequestAsync(uri, HttpMethod.Post, SC.Default.RealtimeNegotiation, content, requestOptions, cancellationToken);
    }
}
