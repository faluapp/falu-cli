using Falu.Client;
using Falu.Client.Realtime;
using Falu.Websockets;

namespace Falu.Commands.Events;

internal partial class EventsListenCommandHandler : ICommandHandler
{
    private readonly FaluCliClient client;
    private readonly ILogger logger;

    public EventsListenCommandHandler(FaluCliClient client, ILogger<EventsListenCommandHandler> logger)
    {
        this.client = client ?? throw new ArgumentNullException(nameof(client));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    int ICommandHandler.Invoke(InvocationContext context) => throw new NotImplementedException();

    public async Task<int> InvokeAsync(InvocationContext context)
    {
        var cancellationToken = context.GetCancellationToken();

        var workspaceId = context.ParseResult.ValueForOption<string>("--workspace")!;
        var live = context.ParseResult.ValueForOption<bool?>("--live") ?? false;
        var types = context.ParseResult.ValueForOption<string[]>("--event-type");

        var filters = new RealtimeConnectionFilters
        {
            Events = new RealtimeConnectionFilterEvents
            {
                Types = types,
            },
        };

        // negotiate a realtime connection
        logger.LogInformation("Negotiating connection information ...");
        var request = new RealtimeConnectionNegotiationRequest { Purpose = "events", };
        var response = await client.Realtime.NegotiateAsync(request, cancellationToken: cancellationToken);
        response.EnsureSuccess();
        var negotiation = response.Resource ?? throw new InvalidOperationException("Response from negotiotion cannot be null or empty");

        Task handleMessage(WebsocketIncomingMessage message, CancellationToken cancellationToken)
        {
            var type = message.Type;
            if (!string.Equals(type, "event", StringComparison.OrdinalIgnoreCase))
            {
                logger.LogWarning("Received unknown message of type {Type}", type);
                return Task.CompletedTask;
            }

            var @object = message.Object ?? throw new InvalidOperationException("The message should have an object at this point");
            var @event = System.Text.Json.JsonSerializer.Deserialize(@object, FaluCliJsonSerializerContext.Default.WebhookEvent)!;
            var eventType = @event.Type;

            logger.LogInformation("--> {EventType} [{EventId}]",
                                  eventType,
                                  @event.Id); // TODO: use a link with format https://dashboard.falu.io/{workspaceId}/developer/events/{requestId}?live={live}

            // TODO: forward the event to a provided destination if any

            return Task.CompletedTask;
        }

        // create handler
        var handler = new WebsocketHandler(negotiation: negotiation,
                                           filters: filters,
                                           handler: handleMessage,
                                           logger: logger);

        // run the handler                  
        await handler.RunAsync(topic: "subscribe_events", cancellationToken);

        return 0;
    }
}
