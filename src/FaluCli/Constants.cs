using System.Reflection;
using System.Text.RegularExpressions;

namespace Falu;

internal partial class Constants
{
    public const string ProductName = "falu-cli";
    public static string Version { get; }
    public static string ProductVersion { get; }


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

    static Constants()
    {
        var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
        Version = assembly.GetName().Version!.ToString();

        /*
         * Use the informational version if available because it has the git commit SHA.
         * Using the git commit SHA allows for maximum reproduction.
         *
         * Examples:
         * 1) 1.7.1-ci.131+Branch.main.Sha.752f6cdfabb76e65d2b2cd18b3b284ef65713213
         * 2) 1.7.1-PullRequest10247.146+Branch.pull-10247-merge.Sha.bf46008b75eacacad3b7654959d38f8df4c7fcdb
         * 3) 1.7.1-fixes-2021-10-12-2.164+Branch.fixes-2021-10-12-2.Sha.bf46008b75eacacad3b7654959d38f8df4c7fcdb
         * 4) 1.9.3+Branch.migration-to-cake.Sha.ed9934bab03eaca1dfcef2c212372f1e6820418e
         *
         * When not available, use the usual assembly version
         */
        var attr = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
        ProductVersion = attr is null ? Version : attr.InformationalVersion;
    }
}
