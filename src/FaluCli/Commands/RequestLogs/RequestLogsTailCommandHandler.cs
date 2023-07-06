using Falu.Client;
using Falu.Client.Realtime;
using Falu.Websockets;
using System.Net;
using System.Text.Json.Serialization;

namespace Falu.Commands.RequestLogs;

internal class RequestLogsTailCommandHandler : ICommandHandler
{
    private readonly FaluCliClient client;
    private readonly ILogger logger;

    public RequestLogsTailCommandHandler(FaluCliClient client, ILogger<RequestLogsTailCommandHandler> logger)
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
        var methods = context.ParseResult.ValueForOption<string[]>("--http-method");
        var paths = context.ParseResult.ValueForOption<string[]>("--request-path");
        var statusCodes = context.ParseResult.ValueForOption<int[]>("--status-code");
        var ipAddresses = context.ParseResult.ValueForOption<IPAddress[]>("--ip-address");
        var sources = context.ParseResult.ValueForOption<string[]>("--source");

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

        // negotiate a realtime connection
        logger.LogInformation("Negotiating connection information ...");
        var request = new RealtimeConnectionNegotiationRequest { Purpose = "logs", };
        var response = await client.Realtime.NegotiateAsync(request, cancellationToken: cancellationToken);
        response.EnsureSuccess();
        var negotiation = response.Resource ?? throw new InvalidOperationException("Response from negotiotion cannot be null or empty");

        Task handleMessage(WebsocketIncomingMessage message, CancellationToken cancellationToken)
        {
            var type = message.Type;
            if (!string.Equals(type, "request_log", StringComparison.OrdinalIgnoreCase))
            {
                logger.LogWarning("Received unknown message of type {Type}", type);
                return Task.CompletedTask;
            }

            var @object = message.Object ?? throw new InvalidOperationException("The message should have an object at this point");
            var log = System.Text.Json.JsonSerializer.Deserialize(@object, FaluCliJsonSerializerContext.Default.RequestLog)!;

            // TODO: log IP, source, and error code if present and based on the filters
            logger.LogInformation("[{StatusCode}] {Method} {Url} [{Id}]",
                                  log.Response.StatusCode,
                                  log.Request.Method,
                                  log.Request.Url,
                                  log.Id); // TODO: use a link with format https://dashboard.falu.io/{workspaceId}/developer/logs/{requestId}?live={live}

            return Task.CompletedTask;
        }

        // create handler
        var handler = new WebsocketHandler(negotiation: negotiation,
                                           filters: filters,
                                           handler: handleMessage,
                                           logger: logger);

        // run the handler                  
        await handler.RunAsync(topic: "subscribe_request_logs", cancellationToken);

        return 0;
    }
}

internal class RequestLog
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = default!;

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
