﻿using System.Text.RegularExpressions;

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
    public static readonly Regex ApiKeyFormat = new(@"^^[s|p]k_(?:live|test)_[0-9a-zA-Z]{20,30}$");
    public static readonly Regex EventIdFormat = new(@"^evt_[a-zA-Z0-9]{20,30}$");
    public static readonly Regex WebhookEndpointIdFormat = new(@"^we_[a-zA-Z0-9]{20,30}$");
    public static readonly Regex MessageTemplateIdFormat = new(@"^(?:mtpl|tmpl)_[a-zA-Z0-9]{20,30}$");
    public static readonly Regex MessageTemplateAliasFormat = new(@"^[a-zA-Z]([a-zA-Z0-9\-_]+)$");
    public static readonly Regex E164PhoneNumberFormat = new(@"^\+[1-9]\d{1,14}$"); // https://ihateregex.io/expr/e164-phone/
}
