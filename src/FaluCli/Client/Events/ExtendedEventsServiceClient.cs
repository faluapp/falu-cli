using Falu.Core;
using Falu.Events;
using System.Net.Http.Json;
using SC = Falu.FaluCliJsonSerializerContext;

namespace Falu.Client.Events;

internal class ExtendedEventsServiceClient(HttpClient backChannel, FaluClientOptions options) : EventsServiceClient(backChannel, options)
{
    public virtual Task<ResourceResponse<WebhookDeliveryAttempt>> RetryAsync(string id,
                                                                             EventDeliveryRetry request,
                                                                             RequestOptions? options = null,
                                                                             CancellationToken cancellationToken = default)
    {
        var uri = MakeResourcePath(id) + "/retry";
        var content = JsonContent.Create(request, SC.Default.EventDeliveryRetry);
        return RequestAsync(uri, HttpMethod.Post, SC.Default.WebhookDeliveryAttempt, content, options, cancellationToken);
    }
}
