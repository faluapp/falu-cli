﻿using CloudNative.CloudEvents;
using CloudNative.CloudEvents.Http;
using Falu.Client;
using Falu.Client.Realtime;
using Falu.Websockets;
using Spectre.Console;
using System.Text;
using System.Text.Json.Nodes;

namespace Falu.Commands.Events;

internal partial class EventsListenCommandHandler : ICommandHandler
{
    private readonly FaluCliClient client;
    private readonly WebsocketHandler websocketHandler;
    private readonly ILogger logger;

    private readonly HttpClientHandler forwardingClientHandler;
    private readonly HttpClient forwardingClient;

    public EventsListenCommandHandler(FaluCliClient client, WebsocketHandler websocketHandler, ILogger<EventsListenCommandHandler> logger)
    {
        this.client = client ?? throw new ArgumentNullException(nameof(client));
        this.websocketHandler = websocketHandler ?? throw new ArgumentNullException(nameof(websocketHandler));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

        forwardingClientHandler = new HttpClientHandler();
        forwardingClient = new HttpClient(forwardingClientHandler);
    }

    int ICommandHandler.Invoke(InvocationContext context) => throw new NotImplementedException();

    public async Task<int> InvokeAsync(InvocationContext context)
    {
        var cancellationToken = context.GetCancellationToken();

        var workspaceId = context.ParseResult.ValueForOption<string>("--workspace")!;
        var live = context.ParseResult.ValueForOption<bool?>("--live") ?? false;
        var webhookEndpointId = context.ParseResult.ValueForOption<string>("--webhook-endpoint");
        var types = context.ParseResult.ValueForOption<string[]>("--event-type")?.NullIfEmpty();
        var forwardTo = context.ParseResult.ValueForOption<Uri?>("--forward-to");
        var skipValidation = context.ParseResult.ValueForOption<bool>("--skip-validation");
        var secret = context.ParseResult.ValueForOption<string?>("--webhook-secret");

        // fetch the webhook endpoint if provided
        Webhooks.WebhookEndpoint? webhookEndpoint = null;
        if (webhookEndpointId is not null)
        {
            logger.LogInformation("Fetching webhook endpoint {WebhookEndpoint} ...", webhookEndpointId);
            var rr = await client.Webhooks.GetAsync(webhookEndpointId, cancellationToken: cancellationToken);
            rr.EnsureSuccess();
            webhookEndpoint = rr.Resource;
            if (webhookEndpoint is null)
            {
                logger.LogWarning("Webhook endpoint '{WebhookEndpointId}' could not be found, or exists in a different live mode or workspace", webhookEndpointId);
                return -1;
            }
        }

        // if there are no types specified and there is a webhook endpoint, we can use it's types
        if ((types is null || types.Length == 0) && webhookEndpoint is not null)
        {
            types = webhookEndpoint.Events?.ToArray() ?? [];
            logger.LogInformation("Filtering event types using the provided webhook:\r\n- {EventTypes}", string.Join("\r\n- ", types));
        }

        // prepare the client to use for forwarding
        if (skipValidation) forwardingClientHandler.ServerCertificateCustomValidationCallback = (_, _, _, _) => true;
        if (forwardTo is not null && secret is null)
        {
            logger.LogWarning("Forwarding without a secret does not test for security concerns."
                            + "\r\nRequests to {ForwardTo} may fail because the 'X-Falu-Signature' header will not be included.",
                              forwardTo);
        }

        // negotiate a realtime connection
        logger.LogInformation("Negotiating connection information ...");
        var request = new RealtimeConnectionNegotiationRequest { Type = "websocket", Purpose = "events", };
        var response = await client.Realtime.NegotiateAsync(request, cancellationToken: cancellationToken);
        response.EnsureSuccess();
        var negotiation = response.Resource ?? throw new InvalidOperationException("Response from negotiation cannot be null or empty");

        // start the handler
        using var cts = negotiation.MakeCancellationTokenSource(cancellationToken);
        cancellationToken = cts.Token;
        await websocketHandler.StartAsync(negotiation, (msg, ct) => HandleIncomingMessage(workspaceId, live, forwardTo, secret, msg, ct), cts);

        // prepare filters
        var filters = new RealtimeConnectionFilters
        {
            Events = new RealtimeConnectionFilterEvents
            {
                Types = types,
            }.NullIfEmpty(),
        }.NullIfEmpty();

        // send message
        var message = new RealtimeConnectionOutgoingMessage("subscribe_events", filters);
        await websocketHandler.SendMessageAsync(message, cancellationToken);

        // run until cancelled
        await Task.Delay(Timeout.Infinite, cancellationToken);

        return 0;
    }

    private Task HandleIncomingMessage(string workspaceId, bool live, Uri? forwardTo, string? secret, RealtimeConnectionIncomingMessage message, CancellationToken cancellationToken)
    {
        var type = message.Type;
        if (!string.Equals(type, "event", StringComparison.OrdinalIgnoreCase))
        {
            logger.LogWarning("Received unknown message of type {Type}", type);
            return Task.CompletedTask;
        }

        var @object = message.Object ?? throw new InvalidOperationException("The message should have an object at this point");
        var @event = System.Text.Json.JsonSerializer.Deserialize(@object, FaluCliJsonSerializerContext.Default.WebhookEvent)!;
        var eventId = @event.Id!;
        var eventType = @event.Type!;
        var eventTypeUrl = DashboardUrlForEventType(workspaceId, live, eventType);
        var eventUrl = DashboardUrlForEvent(workspaceId, live, eventId);

        // write to the console
        // example: 12:48:32  -->  message.delivered [evt_123]
        var sb = new StringBuilder();
        sb.Append(SpectreFormatter.Dim($"{DateTime.Now:T} "));
        sb.Append("  --> ");
        sb.Append(SpectreFormatter.ForLink(text: eventType, url: eventTypeUrl));
        sb.Append(' ');
        sb.Append(SpectreFormatter.EscapeSquares(SpectreFormatter.ForLink(text: eventId, url: eventUrl)));
        AnsiConsole.MarkupLine(sb.ToString());

        // forward the event to a provided destination if any
        if (forwardTo is not null)
        {
            // we do not wait so that we do not block incoming messages
            _ = ForwardAsync(forwardingClient, workspaceId, live, forwardTo, secret, @event, cancellationToken);
        }

        return Task.CompletedTask;
    }

    internal static async Task ForwardAsync(HttpClient client, string workspaceId, bool live, Uri forwardTo, string? secret, Falu.Events.WebhookEvent @event, CancellationToken cancellationToken)
    {
        var eventId = @event.Id!;
        var eventType = @event.Type!;
        var eventUrl = DashboardUrlForEvent(workspaceId, live, eventId);

        var payload = new JsonObject
        {
            ["data"] = new JsonObject
            {
                ["object"] = @event.Data?.Previous,
                ["previous"] = @event.Data?.Previous
            },
            ["request"] = new JsonObject
            {
                ["id"] = @event.Request?.Id,
                ["idempotency_key"] = @event.Request?.IdempotencyKey,
            },
        };
        var cloudEvent = new CloudEvent
        {
            Id = @event.Id, // use the identifier of the event
            Time = @event.Created, // use event creation time
            Source = new Uri(eventUrl),
            Type = $"io.falu.{eventType}", // types must be namespaced/qualified
            DataContentType = System.Net.Mime.MediaTypeNames.Application.Json,
            Data = payload,
            Subject = @event.Data?.Object?["id"]?.ToString(),
        };
        cloudEvent[CloudNative.CloudEvents.Extensions.Falu.WorkspaceAttribute] = workspaceId;
        cloudEvent[CloudNative.CloudEvents.Extensions.Falu.LiveModeAttribute] = live;

        var request = new HttpRequestMessage(HttpMethod.Post, forwardTo);
        var formatter = new CloudNative.CloudEvents.SystemTextJson.JsonEventFormatter();
        request.Content = cloudEvent.ToHttpContent(ContentMode.Structured, formatter);
        var payloadJson = await request.Content.ReadAsStringAsync(cancellationToken);

        // calculate the signature and add to the request headers
        if (secret is not null)
        {
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var signature = ComputeSignature(secret, timestamp, payloadJson);
            request.Headers.Add("X-Falu-Signature", signature);
        }

        // execute the http request
        var start = DateTimeOffset.UtcNow;
        HttpResponseMessage? response;
        try
        {
            response = await client.SendAsync(request, cancellationToken);
        }
        catch (OperationCanceledException) { return; } // nothing to do
        catch (Exception ex)
        {
            // write to console
            AnsiConsole.MarkupLine(SpectreFormatter.ColouredRed($"Error sending webhook: {ex.Message}"));
            return;
        }

        var duration = DateTimeOffset.UtcNow - start;
        var statusCode = (int)response.StatusCode;

        // write to the console
        // example: 12:48:32  <--  [200] POST https://localhost:8080/falu 230.3ms [evt_123]
        var sb = new StringBuilder();
        sb.Append(SpectreFormatter.Dim($"{DateTime.Now:T} "));
        sb.Append("  <--  ");
        sb.Append(SpectreFormatter.EscapeSquares(SpectreFormatter.ForColorizedStatus(statusCode)));
        sb.Append($" {request.Method} {request.RequestUri} ");
        sb.Append($" {duration.TotalMilliseconds:n2} ms ");
        sb.Append(SpectreFormatter.EscapeSquares(SpectreFormatter.ForLink(text: eventId, url: eventUrl)));
        AnsiConsole.MarkupLine(sb.ToString());
    }

    internal static string DashboardUrlForEventType(string workspaceId, bool live, string eventType)
        => $"https://dashboard.falu.io/{workspaceId}/developer/events?type={eventType}&live={live.ToString().ToLowerInvariant()}";

    internal static string DashboardUrlForEvent(string workspaceId, bool live, string eventId)
        => $"https://dashboard.falu.io/{workspaceId}/developer/events/{eventId}?live={live.ToString().ToLowerInvariant()}";

    internal static string ComputeSignature(string secret, long timestamp, string payload)
    {
        var payloadBytes = Encoding.UTF8.GetBytes($"{timestamp}.{payload}");

        using var hasher = new System.Security.Cryptography.HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hasher.ComputeHash(payloadBytes);

        var sha256Value = BitConverter.ToString(hash).Replace("-", string.Empty).ToLowerInvariant();
        return $"t={timestamp},sha256={sha256Value}";
    }
}
