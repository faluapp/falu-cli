using Falu.Client;
using Falu.Client.Realtime;
using Falu.Websockets;
using Spectre.Console;
using System.Net;
using System.Text;
using System.Text.Json.Serialization;

namespace Falu.Commands.RequestLogs;

internal class RequestLogsTailCommandHandler : ICommandHandler
{
    private readonly FaluCliClient client;
    private readonly WebsocketHandler websocketHandler;
    private readonly ILogger logger;

    public RequestLogsTailCommandHandler(FaluCliClient client, WebsocketHandler websocketHandler, ILogger<RequestLogsTailCommandHandler> logger)
    {
        this.client = client ?? throw new ArgumentNullException(nameof(client));
        this.websocketHandler = websocketHandler;
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    int ICommandHandler.Invoke(InvocationContext context) => throw new NotImplementedException();

    public async Task<int> InvokeAsync(InvocationContext context)
    {
        var cancellationToken = context.GetCancellationToken();

        var workspaceId = context.ParseResult.ValueForOption<string>("--workspace")!;
        var live = context.ParseResult.ValueForOption<bool?>("--live") ?? false;
        var methods = context.ParseResult.ValueForOption<string[]>("--http-method");
        var paths = context.ParseResult.ValueForOption<string[]>("--request-path");
        var statusCodes = context.ParseResult.ValueForOption<int[]>("--status-code");
        var ipAddresses = context.ParseResult.ValueForOption<IPAddress[]>("--ip-address");
        var sources = context.ParseResult.ValueForOption<string[]>("--source");

        // negotiate a realtime connection
        logger.LogInformation("Negotiating connection information ...");
        var request = new RealtimeConnectionNegotiationRequest { Purpose = "logs", };
        var response = await client.Realtime.NegotiateAsync(request, cancellationToken: cancellationToken);
        response.EnsureSuccess();
        var negotiation = response.Resource ?? throw new InvalidOperationException("Response from negotiotion cannot be null or empty");

        // start the handler
        using var cts = negotiation.MakeCancellationTokenSource(cancellationToken);
        cancellationToken = cts.Token;
        await websocketHandler.StartAsync(negotiation, (msg, _) => HandleIncomingMessage(workspaceId, live, msg), cts);

        // prepare filters
        var filters = new RealtimeConnectionFilters
        {
            Logs = new RealtimeConnectionFilterLogs
            {
                IPAddresses = ipAddresses,
                Methods = methods,
                Paths = paths,
                Sources = sources,
                StatusCodes = statusCodes,
            },
        };

        // send message
        var message = new WebsocketOutgoingMessage("subscribe_request_logs", filters, negotiation);
        await websocketHandler.SendMessageAsync(message, cancellationToken);

        // run until cancelled
        await Task.Delay(Timeout.Infinite, cancellationToken);

        return 0;
    }

    private Task HandleIncomingMessage(string workspaceId, bool live, WebsocketIncomingMessage message)
    {
        var type = message.Type;
        if (!string.Equals(type, "request_log", StringComparison.OrdinalIgnoreCase))
        {
            logger.LogWarning("Received unknown message of type {Type}", type);
            return Task.CompletedTask;
        }

        var @object = message.Object ?? throw new InvalidOperationException("The message should have an object at this point");
        var log = System.Text.Json.JsonSerializer.Deserialize(@object, FaluCliJsonSerializerContext.Default.RequestLog)!;
        var url = $"https://dashboard.falu.io/{workspaceId}/developer/logs/{log.Id}?live={live.ToString().ToLowerInvariant()}";

        // write to the console
        // example: 12:48:32  [200] POST https://api.falu.io/v1/messages [req_123]
        var sb = new StringBuilder();
        sb.Append(SpectreFormatter.ColouredGrey($"{DateTime.Now:T} "));
        sb.Append(SpectreFormatter.EscapeSquares(SpectreFormatter.ForColorizedStatus(log.Response.StatusCode)));
        sb.Append($" {log.Request.Method} {log.Request.Url} ");
        sb.Append(SpectreFormatter.EscapeSquares(SpectreFormatter.ForLink(text: log.Id, url: url)));
        AnsiConsole.MarkupLine(sb.ToString());

        return Task.CompletedTask;
    }
}

internal class RequestLog
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = default!;

    [JsonPropertyName("created")]
    public DateTimeOffset? Created { get; set; }

    [JsonPropertyName("request")]
    public RequestLogItemRequest Request { get; set; } = default!;

    [JsonPropertyName("response")]
    public RequestLogItemResponse Response { get; set; } = default!;
}

internal class RequestLogItemRequest
{
    [JsonPropertyName("ip")]
    public string IP { get; set; } = default!;

    [JsonPropertyName("method")]
    public string Method { get; set; } = default!;

    [JsonPropertyName("url")]
    public string Url { get; set; } = default!;

    [JsonPropertyName("source")]
    public string Source { get; set; } = default!;
}

internal class RequestLogItemResponse
{
    [JsonPropertyName("status_code")]
    public int StatusCode { get; set; }

    [JsonPropertyName("error_code")]
    public string? ErrorCode { get; set; }
}
