using Falu.Client.Realtime;
using Spectre.Console;
using System.Net;
using System.Text;
using System.Text.Json.Serialization;
using Tingle.Extensions.Primitives;
using Res = Falu.Properties.Resources;

namespace Falu.Commands.RequestLogs;

internal class RequestLogsTailCommand : WorkspacedCommand
{
    private readonly CliOption<IPNetwork[]> ipNetworksOption;
    private readonly CliOption<IPAddress[]> ipAddressesOption;
    private readonly CliOption<string[]> httpMethodsOption;
    private readonly CliOption<string[]> requestPathsOption;
    private readonly CliOption<string[]> sourceOption;
    private readonly CliOption<int[]> statusCodesOption;
    private readonly CliOption<string> ttlOption;

    public RequestLogsTailCommand() : base("tail", "Tail request logs")
    {
        ipNetworksOption = new CliOption<IPNetwork[]>(name: "--ip-network", aliases: ["--network"]) { Description = "The IP network to filter for.", };
        Add(ipNetworksOption);

        ipAddressesOption = new CliOption<IPAddress[]>(name: "--ip-address", aliases: ["--ip"]) { Description = "The IP address to filter for.", };
        Add(ipAddressesOption);

        httpMethodsOption = new CliOption<string[]>(name: "--http-method", aliases: ["--method"]) { Description = "The HTTP method to filter for.", };
        httpMethodsOption.AcceptOnlyFromAmong("get", "patch", "post", "put", "delete");
        Add(httpMethodsOption);

        requestPathsOption = new CliOption<string[]>(name: "--event-type", aliases: ["--type", "-t"]) { Description = "The request path to filter for. For example: \"/v1/messages\"", };
        requestPathsOption.MatchesFormat(Constants.RequestPathWildcardFormat, nulls: true, errorGetter: (v, _) => string.Format(Res.InvalidHttpRequestPath, v));
        Add(requestPathsOption);

        sourceOption = new CliOption<string[]>(name: "--source") { Description = "The request source to filter for.", };
        sourceOption.AcceptOnlyFromAmong("dashboard", "api");
        Add(sourceOption);

        statusCodesOption = new CliOption<int[]>(name: "--status-code") { Description = "The HTTP status code to filter for.", };
        statusCodesOption.IsWithRange(200, 599, nulls: true, errorGetter: (v, _) => string.Format(Res.InvalidHttpStatusCode, v));

        ttlOption = new CliOption<string>(name: "--ttl")
        {
            Description = Res.OptionDescriptionRealtimeConnectionTtl,
            DefaultValueFactory = r => "PT60M",
        };
        ttlOption.IsValidDuration();
        Add(ttlOption);
    }

    public override async Task<int> ExecuteAsync(CliCommandExecutionContext context, CancellationToken cancellationToken)
    {
        var websocketHandler = context.GetRequiredService<WebsocketHandler>();

        var live = GetLiveMode(context.ParseResult) ?? false;
        var ttl = Duration.Parse(context.ParseResult.GetValue(ttlOption)!);
        var ipNetworks = context.ParseResult.GetValue(ipNetworksOption).NullIfEmpty();
        var ipAddresses = context.ParseResult.GetValue(ipAddressesOption).NullIfEmpty();
        var methods = context.ParseResult.GetValue(httpMethodsOption).NullIfEmpty();
        var paths = context.ParseResult.GetValue(requestPathsOption).NullIfEmpty();
        var statusCodes = context.ParseResult.GetValue(statusCodesOption).NullIfEmpty();
        var sources = context.ParseResult.GetValue(sourceOption).NullIfEmpty();

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
        context.Logger.LogInformation("Negotiating connection information ...");
        var response = await context.Client.Realtime.NegotiateAsync(options, cancellationToken: cancellationToken);
        response.EnsureSuccess();
        var negotiation = response.Resource ?? throw new InvalidOperationException("Response from negotiation cannot be null or empty");

        // run the websocket handler
        await websocketHandler.RunAsync(negotiation, HandleIncomingMessage, live, cancellationToken);

        return 0;
    }

    private static ValueTask HandleIncomingMessage(RealtimeMessage message, bool live, CancellationToken cancellationToken = default)
    {
        var @object = message.Object ?? throw new InvalidOperationException("The message should have an object at this point");
        var log = System.Text.Json.JsonSerializer.Deserialize(@object, FaluCliJsonSerializerContext.Default.RequestLog)!;
        var workspaceId = log.Workspace;
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

        [JsonPropertyName("workspace")]
        public string Workspace { get; set; } = default!;
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
}
