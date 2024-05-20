using Falu.Client;
using Falu.Client.Realtime;
using Spectre.Console;
using System.Net;
using System.Text;
using System.Text.Json.Serialization;
using Tingle.Extensions.Primitives;

namespace Falu.Commands.RequestLogs;

internal class RequestLogsTailCommandHandler(FaluCliClient client, WebsocketHandler websocketHandler, ILogger<RequestLogsTailCommandHandler> logger) : ICommandHandler
{
    int ICommandHandler.Invoke(InvocationContext context) => throw new NotImplementedException();

    public async Task<int> InvokeAsync(InvocationContext context)
    {
        var cancellationToken = context.GetCancellationToken();

        var workspaceId = context.ParseResult.ValueForOption<string>("--workspace")!;
        var live = context.ParseResult.ValueForOption<bool?>("--live") ?? false;
        var ttl = Duration.Parse(context.ParseResult.ValueForOption<string>("--ttl")!);
        var ipNetworks = context.ParseResult.ValueForOption<IPNetwork[]>("--ip-network").NullIfEmpty();
        var ipAddresses = context.ParseResult.ValueForOption<IPAddress[]>("--ip-address").NullIfEmpty();
        var methods = context.ParseResult.ValueForOption<string[]>("--http-method").NullIfEmpty();
        var paths = context.ParseResult.ValueForOption<string[]>("--request-path").NullIfEmpty();
        var statusCodes = context.ParseResult.ValueForOption<int[]>("--status-code").NullIfEmpty();
        var sources = context.ParseResult.ValueForOption<string[]>("--source").NullIfEmpty();

        // prepare filters
        var options = new RealtimeNegotiationOptionsRequestLogs
        {
            Ttl = ttl,
            Filters = new RealtimeNegotiationFiltersRequestLogs
            {
                IPNetworks = ipNetworks,
                IPAddresses = ipAddresses,
                Methods = methods,
                Paths = paths,
                StatusCodes = statusCodes,
                Sources = sources,
            },
        };

        // negotiate a connection
        logger.LogInformation("Negotiating connection information ...");
        var response = await client.Realtime.NegotiateAsync(options, cancellationToken: cancellationToken);
        response.EnsureSuccess();
        var negotiation = response.Resource ?? throw new InvalidOperationException("Response from negotiation cannot be null or empty");

        // run the websocket handler
        var arg = new HandlerArg(workspaceId, live);
        await websocketHandler.RunAsync(negotiation, HandleIncomingMessage, arg, cancellationToken);

        return 0;
    }

    private record struct HandlerArg(string WorkspaceId, bool Live)
    {
        public readonly void Deconstruct(out string workspaceId, out bool live)
        {
            workspaceId = WorkspaceId;
            live = Live;
        }
    }

    private static ValueTask HandleIncomingMessage(RealtimeMessage message, HandlerArg arg, CancellationToken cancellationToken = default)
    {
        var (workspaceId, live) = arg;
        var @object = message.Object ?? throw new InvalidOperationException("The message should have an object at this point");
        var log = System.Text.Json.JsonSerializer.Deserialize(@object, FaluCliJsonSerializerContext.Default.RequestLog)!;
        var url = $"https://dashboard.falu.io/{workspaceId}/developer/logs/{log.Id}?live={live.ToString().ToLowerInvariant()}";

        // write to the console
        // example: 12:48:32  [200] POST /v1/messages [req_123]
        var sb = new StringBuilder();
        sb.Append(SpectreFormatter.Dim($"{DateTime.Now:T} "));
        sb.Append(SpectreFormatter.EscapeSquares(SpectreFormatter.ForColorizedStatus(log.Response.StatusCode)));
        sb.Append($" {log.Request.Method} {log.Request.Url} ");
        sb.Append(SpectreFormatter.EscapeSquares(SpectreFormatter.ForLink(text: log.Id, url: url)));
        AnsiConsole.MarkupLine(sb.ToString());

        return ValueTask.CompletedTask;
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
