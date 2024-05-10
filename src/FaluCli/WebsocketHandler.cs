using Falu.Client.Realtime;
using System.Net.WebSockets;
using System.Text.Json;
using SC = Falu.FaluCliJsonSerializerContext;

namespace Falu;

internal class WebsocketHandler(ILogger<WebsocketHandler> logger)
{
    /// <summary>Handler for incoming messages.</summary>
    /// <param name="message">The message to be handled.</param>
    /// <param name="cancellationToken">
    /// A cancellation token used to propagate notification that the operation should be canceled.
    /// </param>
    public delegate ValueTask MessageHandler(RealtimeMessage message, CancellationToken cancellationToken);

    /// <summary>Handler for incoming messages with state.</summary>
    /// <typeparam name="TArg"></typeparam>
    /// <param name="message">The message to be handled.</param>
    /// <param name="arg">The arg to be passed to the handler.</param>
    /// <param name="cancellationToken">
    /// A cancellation token used to propagate notification that the operation should be canceled.
    /// </param>
    public delegate ValueTask MessageHandler<TArg>(RealtimeMessage message, TArg? arg, CancellationToken cancellationToken);

    public Task RunAsync(RealtimeNegotiation negotiation, MessageHandler handler, CancellationToken cancellationToken = default)
        => RunAsync<object>(negotiation, (msg, _, ct) => handler(msg, ct), arg: null, cancellationToken);

    public async Task RunAsync<TArg>(RealtimeNegotiation negotiation, MessageHandler<TArg> handler, TArg? arg, CancellationToken cancellationToken = default)
    {
        // create a CancellationToken sourced from the other and cancels when the token expires
        var lifetime = negotiation.Expires - DateTimeOffset.UtcNow - TimeSpan.FromSeconds(2);
        var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(lifetime);
        cancellationToken = cts.Token;

        var url = negotiation.Url;
        var token = negotiation.Token;
        var state = negotiation.State;
        logger.LogInformation("Opening websocket connection to {Url}", url);
        logger.LogInformation("Connection valid for {Minutes} minutes", Convert.ToInt32((negotiation.Expires - DateTimeOffset.UtcNow).TotalMinutes));
        logger.LogDebug("Connection token:\r\n{Token}", token);
        logger.LogDebug("Connection state:\r\n{State}", state);

        // create client socket
        var socket = new ClientWebSocket();
        socket.Options.AddSubProtocol("json.devproxy.falu.v1");
        socket.Options.SetRequestHeader("Authorization", $"Bearer {token}");
        socket.Options.SetRequestHeader("X-Negotiated-State", state);

        // connect
        await socket.ConnectAsync(url, cancellationToken);
        logger.LogInformation("Connected to websocket server. It may take a few seconds for the data to start flowing.");

        // listen to incoming messages
        var buffer = new byte[10 * 1024];
        while (!cancellationToken.IsCancellationRequested)
        {
            // receive data from the socket
            var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);

            // check for closure
            if (result.CloseStatus is not null)
            {
                var status = result.CloseStatus.Value;
                var description = result.CloseStatusDescription;
                logger.LogDebug("WebSocket Connection closed. Status: {CloseStatus}, Description: {CloseStatusDescription}", status, description);
                await socket.CloseAsync(status, description, cancellationToken);
                return;
            }

            // check for close message
            if (result.MessageType is WebSocketMessageType.Close)
            {
                logger.LogDebug("WebSocket Connection sent a close message.");
                await socket.CloseAsync(WebSocketCloseStatus.Empty, null, cancellationToken: cancellationToken);
                return;
            }

            // ensure we have full messages
            if (!result.EndOfMessage)
            {
                logger.LogWarning("WebSocket server received a partial message that we do not support. This should not happen." +
                                  " Raise an issue On GitHub at https://github.com/faluapp/falu-cli/issues");
                continue;
            }

            // at this point we have a binary message

            // Take only the data read and invoke the handler
            var data = BinaryData.FromBytes(buffer[..result.Count]);
            logger.LogDebug("Received message: {Data}", data);
            var message = JsonSerializer.Deserialize(data, SC.Default.RealtimeMessage) ?? throw new InvalidOperationException("Unable to desrialize incoming message");
            await handler(message, arg, cancellationToken);
        }
    }
}
