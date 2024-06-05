using System.Text.RegularExpressions;

namespace Falu;

internal partial class Constants
{
    public const string ProductName = "falu-cli";

    // this value is hardcoded because Microsoft does not consider the instrumentation key sensitive
    public const string AppInsightsConnectionString = "InstrumentationKey=05728099-c2aa-411d-8f1c-e3aa9689daae;IngestionEndpoint=https://westeurope-5.in.applicationinsights.azure.com/;LiveEndpoint=https://westeurope.livediagnostics.monitor.azure.com/;ApplicationId=bb8bb015-675d-4658-a286-5d2108ca437a";

    public const string RepositoryOwner = "faluapp";
    public const string RepositoryName = "falu-cli";

    public const string Authority = "https://sts.falu.io";
    public const string DeviceAuthorizationEndpoint = $"{Authority}/connect/deviceauthorization";
    public const string TokenEndpoint = $"{Authority}/connect/token";
    public const string ClientId = "cli";
    public static readonly string Scopes = string.Join(" ", ["openid", "offline_access", "api"]);

    public static readonly ByteSizeLib.ByteSize MaxStatementFileSize = ByteSizeLib.ByteSize.FromKibiBytes(256);
    public static readonly string MaxStatementFileSizeString = MaxStatementFileSize.ToBinaryString();

    public static readonly Regex IdempotencyKeyFormat = GetIdempotencyKeyFormat();
    public static readonly Regex ApiKeyFormat = GetApiKeyFormat();
    public static readonly Regex EventIdFormat = GetEventIdFormat();
    public static readonly Regex EventTypeWildcardFormat = GetEventTypeWildcardFormat();
    public static readonly Regex WebhookEndpointIdFormat = GetWebhookEndpointIdFormat();
    public static readonly Regex MessageTemplateIdFormat = GetMessageTemplateIdFormat();
    public static readonly Regex MessageTemplateAliasFormat = GetMessageTemplateAliasFormat();
    public static readonly Regex FileIdFormat = GetFileIdFormat();
    public static readonly Regex E164PhoneNumberFormat = GetE164PhoneNumberFormat();
    public static readonly Regex RequestPathWildcardFormat = GetRequestPathWildcardFormat();

    [GeneratedRegex("^[a-zA-Z0-9-_:]{2,128}$")]
    private static partial Regex GetIdempotencyKeyFormat();

    [GeneratedRegex("^f[s|p|t]k[l|t]_[0-9a-zA-Z]{20,50}$")]
    private static partial Regex GetApiKeyFormat();

    [GeneratedRegex("^evt_[a-zA-Z0-9]{20,30}$")]
    private static partial Regex GetEventIdFormat();

    [GeneratedRegex("^[a-zA-Z0-9-._*]+$")]
    private static partial Regex GetEventTypeWildcardFormat();

    [GeneratedRegex("^we_[a-zA-Z0-9]{20,30}$")]
    private static partial Regex GetWebhookEndpointIdFormat();

    [GeneratedRegex("^(?:mtpl|tmpl)_[a-zA-Z0-9]{20,30}$")]
    private static partial Regex GetMessageTemplateIdFormat();

    [GeneratedRegex("^[a-zA-Z]([a-zA-Z0-9\\-_]+)$")]
    private static partial Regex GetMessageTemplateAliasFormat();

    [GeneratedRegex("^file_[a-zA-Z0-9]{20,30}$")]
    private static partial Regex GetFileIdFormat();

    [GeneratedRegex("^\\+[1-9]\\d{1,14}$")]
    private static partial Regex GetE164PhoneNumberFormat(); // https://ihateregex.io/expr/e164-phone/

    [GeneratedRegex("^[a-zA-Z0-9-_/\\*]+$")]
    private static partial Regex GetRequestPathWildcardFormat();
}
