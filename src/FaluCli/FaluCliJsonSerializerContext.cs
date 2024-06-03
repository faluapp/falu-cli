using System.Text.Json;
using System.Text.Json.Serialization;

namespace Falu;

[JsonSerializable(typeof(Events.WebhookEvent))]
[JsonSerializable(typeof(Client.Events.EventDeliveryRetry))]
[JsonSerializable(typeof(Client.Events.WebhookDeliveryAttempt))]

[JsonSerializable(typeof(List<Client.MoneyStatements.MoneyStatement>))]
[JsonSerializable(typeof(Client.MoneyStatements.MoneyStatementUploadResponse))]

[JsonSerializable(typeof(Client.Realtime.RealtimeMessage))]
[JsonSerializable(typeof(Client.Realtime.RealtimeNegotiationOptionsEvents))]
[JsonSerializable(typeof(Client.Realtime.RealtimeNegotiationOptionsRequestLogs))]
[JsonSerializable(typeof(Client.Realtime.RealtimeNegotiation))]

[JsonSerializable(typeof(Commands.RequestLogs.RequestLogsTailCommand.RequestLog))]
[JsonSerializable(typeof(Commands.Templates.TemplateInfo))]

[JsonSerializable(typeof(Oidc.OidcDeviceAuthorizationResponse))]
[JsonSerializable(typeof(Oidc.OidcTokenResponse))]

[JsonSerializable(typeof(GitHubLatestRelease))]

[JsonSourceGenerationOptions(
    AllowTrailingCommas = true,
    ReadCommentHandling = JsonCommentHandling.Skip,

    // Ignore default values to reduce the data sent after serialization
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,

    // Do not indent content to reduce data usage
    WriteIndented = false,

    // Use SnakeCase because it is what the server provides
    PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower,
    DictionaryKeyPolicy = JsonKnownNamingPolicy.Unspecified
)]
internal partial class FaluCliJsonSerializerContext : JsonSerializerContext { }
