using Falu.Core;
using System.Net.Http.Json;
using SC = Falu.FaluCliJsonSerializerContext;

namespace Falu.Client.Realtime;

internal class RealtimeServiceClient(HttpClient backChannel, FaluClientOptions options) : BaseServiceClient(backChannel, options)
{
    public virtual Task<ResourceResponse<RealtimeConnectionNegotiation>> NegotiateAsync(RealtimeConnectionNegotiationRequest request,
                                                                                        RequestOptions? options = null,
                                                                                        CancellationToken cancellationToken = default)
    {
        var uri = "/v1/realtime/negotiate";
        var content = JsonContent.Create(request, SC.Default.RealtimeConnectionNegotiationRequest);
        return RequestAsync(uri, HttpMethod.Post, SC.Default.RealtimeConnectionNegotiation, content, options, cancellationToken);
    }
}
