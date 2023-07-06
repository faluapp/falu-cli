using Falu.Client.Realtime;
using System.Net.WebSockets;
using System.Text.Json;
using Websocket.Client;
using SC = Falu.FaluCliJsonSerializerContext;

namespace Falu.Websockets;

internal class WebsocketHandler
{
    private readonly RealtimeConnectionNegotiation negotiation;
    private readonly RealtimeConnectionFilters filters;
    private readonly Func<WebsocketIncomingMessage, CancellationToken, Task> handler;
    private readonly ILogger logger;

    public WebsocketHandler(RealtimeConnectionNegotiation negotiation,
                            RealtimeConnectionFilters filters,
                            Func<WebsocketIncomingMessage, CancellationToken, Task> handler,
                            ILogger logger)
    {
        this.negotiation = negotiation ?? throw new ArgumentNullException(nameof(negotiation));
        this.filters = filters ?? throw new ArgumentNullException(nameof(filters));
        this.handler = handler ?? throw new ArgumentNullException(nameof(handler));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task RunAsync(string? topic, CancellationToken cancellationToken = default)
    {
        var remainingTime = negotiation.Expires - DateTimeOffset.UtcNow - TimeSpan.FromSeconds(2);
        using var expiryCts = new CancellationTokenSource(remainingTime);
        expiryCts.Token.Register(() => logger.LogInformation("Closing the connection because the token expired."));
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, expiryCts.Token);
        cancellationToken = cts.Token;

        var url = negotiation.Url;
        var maskedUrl = MaskAccessToken(url);
        var token = negotiation.Token;
        logger.LogInformation("Opening websocket connection to {Url}", maskedUrl);
        logger.LogInformation("Connection valid for {Minutes} minutes", Convert.ToInt32(remainingTime.TotalMinutes));
        logger.LogDebug("Connection token:\r\n{Token}", token);

        ClientWebSocket factory()
        {
            var inner = new ClientWebSocket();
            inner.Options.AddSubProtocol("json.devproxy.falu.v1");

            if (!url.ToString().Contains(token))
            {
                inner.Options.SetRequestHeader("Authorization", token);
            }

            return inner;
        }

        // create client, connect and subscribe
        using var wsClient = new WebsocketClient(url, factory);
        wsClient.ReconnectTimeout = null; // disable auto disconnect and reconnect becase we want to stay online even when no data comes in
        wsClient.MessageReceived.Subscribe(rm =>
        {
            // when the server closes the connection, cancel the token
            var type = rm.MessageType;
            if (type is WebSocketMessageType.Close)
            {
                logger.LogInformation("Server closed the websocket");
                cts.Cancel();
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
        await wsClient.StartOrFail();
        logger.LogInformation("Connected to websocket server.");

        // subscribe to the topic if supplied
        if (topic is not null)
        {
            var message = new WebsocketOutgoingMessage
            {
                Type = topic,
                Filters = filters,
                Workspace = negotiation.Workspace,
                Live = negotiation.Live,
            };
            var subMessageJson = JsonSerializer.Serialize(message, SC.Default.WebsocketOutgoingMessage);
            logger.LogDebug("Sending message: {Data}", subMessageJson);
            wsClient.Send(subMessageJson);
        }

        // wait for cancellation
        try { await Task.Delay(Timeout.Infinite, cancellationToken); }
        catch (TaskCanceledException) when (cancellationToken.IsCancellationRequested) { } // ignore user cancels or URL expires
    }

    private static string MaskAccessToken(Uri url, string key = "access_token")
    {
        var query = url.Query;
        if (string.IsNullOrWhiteSpace(query)) return url.ToString();

        var dict = query.TrimStart('?').Split('&').Select(q => q.Split('=')).ToDictionary(q => q.First(), q => q.Last(), StringComparer.OrdinalIgnoreCase);
        if (dict.TryGetValue(key, out _))
        {
            dict[key] = "***";
        }

        query = $"?{string.Join('&', dict.Select(p => $"{p.Key}={p.Value}"))}";
        return new UriBuilder(url) { Query = query, }.Uri.ToString();
    }
}
