using Falu.Client.Realtime;
using System.Net.WebSockets;
using System.Text.Json;
using Websocket.Client;
using SC = Falu.FaluCliJsonSerializerContext;

namespace Falu.Websockets;

internal class WebsocketHandler : IDisposable
{
    private readonly ILogger logger;

    private WebsocketClient? client;

    public WebsocketHandler(ILogger<WebsocketHandler> logger)
    {
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task StartAsync(RealtimeConnectionNegotiation negotiation,
                                 Func<WebsocketIncomingMessage, CancellationToken, Task> handler,
                                 CancellationTokenSource cancellationTokenSource)
    {
        var cancellationToken = cancellationTokenSource.Token;
        var url = negotiation.Url;
        var token = negotiation.Token;
        logger.LogInformation("Opening websocket connection to {Url}", url);
        logger.LogInformation("Connection valid for {Minutes} minutes", Convert.ToInt32((negotiation.Expires - DateTimeOffset.UtcNow).TotalMinutes));
        logger.LogDebug("Connection token:\r\n{Token}", token);

        ClientWebSocket factory()
        {
            var inner = new ClientWebSocket();
            inner.Options.AddSubProtocol("json.devproxy.falu.v1");
            inner.Options.SetRequestHeader("Authorization", $"Bearer {token}");
            return inner;
        }

        // create client
        client = new WebsocketClient(url, factory)
        {
            ReconnectTimeout = null, // disable auto disconnect and reconnect becase we want to stay online even when no data comes in
        };

        // subscribe to incoming messages
        client.MessageReceived.Subscribe(rm =>
        {
            // when the server closes the connection, cancel the token
            var type = rm.MessageType;
            if (type is WebSocketMessageType.Close)
            {
                logger.LogInformation("Server closed the websocket");
                cancellationTokenSource.Cancel();
            }

            // create BinaryData for ease of manipulation and logging
            var data = type switch
            {
                WebSocketMessageType.Binary => BinaryData.FromBytes(rm.Binary),
                WebSocketMessageType.Text => BinaryData.FromString(rm.Text),
                _ => throw new InvalidOperationException($"Unknown message type '{nameof(WebSocketMessageType)}.{type}'")
            };
            logger.LogDebug("Received message: {Data}", data);

            // decode the message from JSON and send to handler
            var message = JsonSerializer.Deserialize(data, SC.Default.WebsocketIncomingMessage) ?? throw new InvalidOperationException("Unable to desrialize incoming message");
            handler(message, cancellationToken);
        });

        // connect
        await client.StartOrFail();
        logger.LogInformation("Connected to websocket server.");
    }

    public Task SendMessageAsync(WebsocketOutgoingMessage message, CancellationToken cancellationToken = default)
    {
        var client = GetClient();
        var json = JsonSerializer.Serialize(message, SC.Default.WebsocketOutgoingMessage);
        logger.LogDebug("Sending message: {Data}", json);
        client.Send(json);
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        client?.Dispose();
    }

    private WebsocketClient GetClient()
    {
        if (client is null)
        {
            throw new InvalidOperationException($"The websocket client has not been initialized. '{nameof(StartAsync)}(...)' must be called first.");
        }

        return client;
    }
}
