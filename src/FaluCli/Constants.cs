using System.Text.RegularExpressions;

namespace Falu;

internal partial class Constants
{
    public const string RepositoryOwner = "faluapp";
    public const string RepositoryName = "falu-cli";

    public const string Authority = "https://login.falu.io";
    public const string DeviceAuthorizationEndpoint = $"{Authority}/connect/deviceauthorization";
    public const string TokenEndpoint = $"{Authority}/connect/token";
    public const string ClientId = "cli";
    public static readonly string Scopes = string.Join(" ", new[] { "openid", "offline_access", "api", });

    public static readonly ByteSizeLib.ByteSize MaxStatementFileSize = ByteSizeLib.ByteSize.FromKibiBytes(256);
    public static readonly string MaxStatementFileSizeString = MaxStatementFileSize.ToBinaryString();

    public static readonly Regex WorkspaceIdFormat = GetWorkspaceIdFormat();
    public static readonly Regex IdempotencyKeyFormat = GetIdempotencyKeyFormat();
    public static readonly Regex ApiKeyFormat = GetApiKeyFormat();
    public static readonly Regex EventIdFormat = GetEventIdFormat();
    public static readonly Regex EventTypeWildcardFormat = GetEventTypeWildcardFormat();
    public static readonly Regex WebhookEndpointIdFormat = GetWebhookEndpointIdFormat();
    public static readonly Regex MessageTemplateIdFormat = GetMessageTemplateIdFormat();
    public static readonly Regex MessageTemplateAliasFormat = GetMessageTemplateAliasFormat();
    public static readonly Regex FileIdFormat = GetFileIdFormat();
    public static readonly Regex E164PhoneNumberFormat = GetE164PhoneNumberFormat();
    public static readonly Regex Iso8061DurationFormat = GetIso8061DurationFormat();
    public static readonly Regex RequestPathWildcardFormat = GetRequestPathWildcardFormat();

    [GeneratedRegex("^wksp_[a-zA-Z0-9]{20,30}$")]
    private static partial Regex GetWorkspaceIdFormat();

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

    [GeneratedRegex("^P(([0-9.,]+Y)?([0-9.,]+M)?([0-9.,]+W)?([0-9.,]+D)?)(T([0-9.,]+H)?([0-9.,]+M)?([0-9.,]+S)?)?$")]
    private static partial Regex GetIso8061DurationFormat(); // https://gist.github.com/tristanls/356ce5aea0054b770d49

    [GeneratedRegex("^[a-zA-Z0-9-_/\\*]+$")]
    private static partial Regex GetRequestPathWildcardFormat();
}
