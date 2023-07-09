using Falu.Core;
using SC = Falu.FaluCliJsonSerializerContext;

namespace Falu.Client.Realtime;

internal class RealtimeServiceClient : BaseServiceClient
{
    public RealtimeServiceClient(HttpClient backChannel, FaluClientOptions options) : base(backChannel, options) { }

    public virtual Task<ResourceResponse<RealtimeConnectionNegotiation>> NegotiateAsync(RealtimeConnectionNegotiationRequest request,
                                                                                        RequestOptions? options = null,
                                                                                        CancellationToken cancellationToken = default)
    {
        var uri = "/v1/realtime/negotiate";
        var content = FaluJsonContent.Create(request, SC.Default.RealtimeConnectionNegotiationRequest);
        return RequestAsync(uri, HttpMethod.Post, SC.Default.RealtimeConnectionNegotiation, content, options, cancellationToken);
    }
}
