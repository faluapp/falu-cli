using Falu.Client.Realtime;
using System.Net.WebSockets;
using System.Text.Json;
using SC = Falu.FaluCliJsonSerializerContext;

namespace Falu;

internal class WebsocketHandler(ILogger<WebsocketHandler> logger) : IDisposable
{
    private ClientWebSocket? socket;

    public async Task StartAsync(RealtimeNegotiation negotiation,
                                 Func<RealtimeMessage, CancellationToken, Task> handler,
                                 CancellationTokenSource cancellationTokenSource)
    {
        var cancellationToken = cancellationTokenSource.Token;
        var url = negotiation.Url;
        var token = negotiation.Token;
        var state = negotiation.State;
        logger.LogInformation("Opening websocket connection to {Url}", url);
        logger.LogInformation("Connection valid for {Minutes} minutes", Convert.ToInt32((negotiation.Expires - DateTimeOffset.UtcNow).TotalMinutes));
        logger.LogDebug("Connection token:\r\n{Token}", token);
        logger.LogDebug("Connection state:\r\n{State}", state);

        // create client socket
        socket = new ClientWebSocket();
        socket.Options.AddSubProtocol("json.devproxy.falu.v1");
        socket.Options.SetRequestHeader("Authorization", $"Bearer {token}");
        socket.Options.SetRequestHeader("X-Negotiated-State", state);

        // connect
        await socket.ConnectAsync(url, cancellationToken);
        logger.LogInformation("Connected to websocket server.");

        // run without blocking the caller
        _ = RunAsync(socket, handler, cancellationToken);
    }

    private async Task RunAsync(ClientWebSocket socket, Func<RealtimeMessage, CancellationToken, Task> handler, CancellationToken cancellationToken)
    {
        // listen to incoming messages
        var buffer = new byte[1024];
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
                logger.LogWarning("WebSocket Connection sent a partial message that we do not support. Closing");
                await socket.CloseAsync(WebSocketCloseStatus.InvalidPayloadData, "The whole message must be sent once.", cancellationToken);
                return;
            }

            // at this point we have a binary message

            // Take only the data read and invoke the handler
            var data = BinaryData.FromBytes(buffer[..result.Count]);
            logger.LogDebug("Received message: {Data}", data);
            var message = JsonSerializer.Deserialize(data, SC.Default.RealtimeMessage) ?? throw new InvalidOperationException("Unable to desrialize incoming message");
            await handler(message, cancellationToken);
        }
    }

    public void Dispose() => socket?.Dispose();

    private ClientWebSocket GetSocket()
    {
        if (socket is null)
        {
            throw new InvalidOperationException($"The websocket client has not been initialized. '{nameof(StartAsync)}(...)' must be called first.");
        }

        return socket;
    }
}
