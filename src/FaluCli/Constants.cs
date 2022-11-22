using System.Text.RegularExpressions;

namespace Falu;

internal class Constants
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

    public static readonly ByteSizeLib.ByteSize MaxMpesaStatementFileSize = ByteSizeLib.ByteSize.FromKibiBytes(128);
    public static readonly string MaxMpesaStatementFileSizeString = MaxMpesaStatementFileSize.ToBinaryString();

    public static readonly Regex WorkspaceIdFormat = new(@"^wksp_[a-zA-Z0-9]{20,30}$");
    public static readonly Regex IdempotencyKeyFormat = new (@"^[a-zA-Z0-9-_:]{2,128}$");
    public static readonly Regex ApiKeyFormat = new(@"^^f[s|p]k[l|t]_[0-9a-zA-Z]{20,30}$");
    public static readonly Regex EventIdFormat = new(@"^evt_[a-zA-Z0-9]{20,30}$");
    public static readonly Regex WebhookEndpointIdFormat = new(@"^we_[a-zA-Z0-9]{20,30}$");
    public static readonly Regex MessageTemplateIdFormat = new(@"^(?:mtpl|tmpl)_[a-zA-Z0-9]{20,30}$");
    public static readonly Regex MessageTemplateAliasFormat = new(@"^[a-zA-Z]([a-zA-Z0-9\-_]+)$");
    public static readonly Regex FileIdFormat = new(@"^file_[a-zA-Z0-9]{20,30}$");
    public static readonly Regex E164PhoneNumberFormat = new(@"^\+[1-9]\d{1,14}$"); // https://ihateregex.io/expr/e164-phone/
    public static readonly Regex Iso8061DurationFormat = new(@"^P(([0-9.,]+Y)?([0-9.,]+M)?([0-9.,]+W)?([0-9.,]+D)?)(T([0-9.,]+H)?([0-9.,]+M)?([0-9.,]+S)?)?$"); // https://gist.github.com/tristanls/356ce5aea0054b770d49
}
