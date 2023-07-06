﻿using System.Text.Json;
using System.Text.Json.Serialization;

namespace Falu;

[JsonSerializable(typeof(Events.WebhookEvent))]
[JsonSerializable(typeof(Client.Events.EventDeliveryRetry))]
[JsonSerializable(typeof(Client.Events.WebhookDeliveryAttempt))]

[JsonSerializable(typeof(List<Client.MoneyStatements.ExtractedStatementRecord>))]

[JsonSerializable(typeof(Client.Realtime.RealtimeConnectionNegotiationRequest))]
[JsonSerializable(typeof(Client.Realtime.RealtimeConnectionNegotiation))]

[JsonSerializable(typeof(Commands.RequestLogs.RequestLog))]
[JsonSerializable(typeof(Commands.Templates.TemplateInfo))]
[JsonSerializable(typeof(Config.ConfigValues))]

[JsonSerializable(typeof(Oidc.OidcDeviceAuthorizationResponse))]
[JsonSerializable(typeof(Oidc.OidcTokenResponse))]

[JsonSerializable(typeof(Updates.GitHubLatestRelease))]
[JsonSerializable(typeof(Websockets.WebsocketIncomingMessage))]
[JsonSerializable(typeof(Websockets.WebsocketOutgoingMessage))]
internal partial class FaluCliJsonSerializerContext : JsonSerializerContext
{
    private static JsonSerializerOptions DefaultSerializerOptions { get; } = new(JsonSerializerDefaults.Web)
    {
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip,

        // Ignore default values to reduce the data sent after serialization
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,

        // Do not indent content to reduce data usage
        WriteIndented = false,

        // TODO; change to snake case in .NET 8
        //// Use SnakeCase because it is what the server provides
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DictionaryKeyPolicy = null,
    };

    static FaluCliJsonSerializerContext() => s_defaultContext = new FaluCliJsonSerializerContext(new JsonSerializerOptions(DefaultSerializerOptions));
}