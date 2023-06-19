using System.Text.RegularExpressions;

namespace Falu;

internal partial class Constants
{
    public const string OpenIdCategoryOrClientName = "Oidc";

    public const string RepositoryOwner = "tinglesoftware";
    public const string RepositoryName = "falu-cli";

    public const string Authority = "https://login.falu.io";
    public const string ScopeApi = "api";
    public const string ClientId = "cli";
    public static readonly ICollection<string> ScopesList = new HashSet<string>
    {
        IdentityModel.OidcConstants.StandardScopes.OpenId,
        IdentityModel.OidcConstants.StandardScopes.OfflineAccess,
        ScopeApi,
    };
    public static readonly string Scopes = string.Join(" ", ScopesList);

    public static readonly ByteSizeLib.ByteSize MaxMpesaStatementFileSize = ByteSizeLib.ByteSize.FromKibiBytes(256);
    public static readonly string MaxMpesaStatementFileSizeString = MaxMpesaStatementFileSize.ToBinaryString();

    public static readonly Regex WorkspaceIdFormat = GetWorkspaceIdFormat();
    public static readonly Regex IdempotencyKeyFormat = GetIdempotencyKeyFormat();
    public static readonly Regex ApiKeyFormat = GetApiKeyFormat();
    public static readonly Regex EventIdFormat = GetEventIdFormat();
    public static readonly Regex WebhookEndpointIdFormat = GetWebhookEndpointIdFormat();
    public static readonly Regex MessageTemplateIdFormat = GetMessageTemplateIdFormat();
    public static readonly Regex MessageTemplateAliasFormat = GetMessageTemplateAliasFormat();
    public static readonly Regex FileIdFormat = GetFileIdFormat();
    public static readonly Regex E164PhoneNumberFormat = GetE164PhoneNumberFormat(); // https://ihateregex.io/expr/e164-phone/
    public static readonly Regex Iso8061DurationFormat = GetIso8061DurationFormat();

    [GeneratedRegex("^wksp_[a-zA-Z0-9]{20,30}$")]
    private static partial Regex GetWorkspaceIdFormat();

    [GeneratedRegex("^[a-zA-Z0-9-_:]{2,128}$")]
    private static partial Regex GetIdempotencyKeyFormat();

    [GeneratedRegex("^f[s|p]k[l|t]_[0-9a-zA-Z]{20,30}$")]
    private static partial Regex GetApiKeyFormat();

    [GeneratedRegex("^evt_[a-zA-Z0-9]{20,30}$")]
    private static partial Regex GetEventIdFormat();

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
}
