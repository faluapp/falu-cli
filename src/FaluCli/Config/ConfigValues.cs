using Falu.Oidc;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Falu.Config;

internal sealed class ConfigValues(JsonObject inner) : AbstractConfigValues(inner)
{
    public const int DefaultRetries = 0;
    public const int DefaultTimeout = 120;

    private const string KeyNoTelemetry = "no_telemetry";
    private const string KeyNoUpdates = "no_updates";
    private const string KeyLastUpdateCheck = "last_update_check";
    private const string KeyRetries = "retries";
    private const string KeyTimeout = "timeout";
    private const string KeyDefaultWorkspaceId = "default_workspace_id";
    private const string KeyDefaultLiveMode = "default_live_mode";
    private const string KeyAuthentication = "authentication";

    public bool NoTelemetry { get => GetPrimitiveValue(KeyNoTelemetry, false); set => SetValue(KeyNoTelemetry, value); }
    public bool NoUpdates { get => GetPrimitiveValue(KeyNoUpdates, false); set => SetValue(KeyNoUpdates, value); }
    public DateTimeOffset? LastUpdateCheck { get => GetPrimitiveValue<DateTimeOffset>(KeyLastUpdateCheck); set => SetValue(KeyLastUpdateCheck, value); }
    public int Retries { get => GetPrimitiveValue(KeyRetries, DefaultRetries); set => SetValue(KeyRetries, value); }
    public int Timeout { get => GetPrimitiveValue(KeyTimeout, DefaultTimeout); set => SetValue(KeyTimeout, value); }
    public string? DefaultWorkspaceId { get => GetValue(KeyDefaultWorkspaceId, (string?)null); set => SetValue(KeyDefaultWorkspaceId, value); }
    public bool? DefaultLiveMode { get => GetPrimitiveValue<bool>(KeyDefaultLiveMode); set => SetValue(KeyDefaultLiveMode, value); }

    public ConfigValuesAuthenticationTokens? Authentication
    {
        get => GetObject(KeyAuthentication);
        set => SetValue(KeyAuthentication, value);
    }

    public string Json(JsonSerializerOptions serializerOptions) => Inner.ToJsonString(serializerOptions);
}

internal sealed class ConfigValuesAuthenticationTokens : AbstractConfigValues
{
    private const string KeyAccessToken = "access_token";
    private const string KeyAccessTokenExpiry = "access_token_expiry";
    private const string KeyRefreshToken = "refresh_token";

    public ConfigValuesAuthenticationTokens(JsonObject inner) : base(inner) { }

    public ConfigValuesAuthenticationTokens(OidcTokenResponse response) : base([])
    {
        AccessToken = response.AccessToken;
        RefreshToken = response.RefreshToken;
        AccessTokenExpiry = DateTimeOffset.UtcNow.AddSeconds(response.ExpiresIn).AddSeconds(-5);
    }

    public string? AccessToken { get => GetValue(KeyAccessToken, (string?)null); set => SetValue(KeyAccessToken, value); }
    public DateTimeOffset? AccessTokenExpiry { get => GetPrimitiveValue<DateTimeOffset>(KeyAccessTokenExpiry); set => SetValue(KeyAccessTokenExpiry, value); }
    public string? RefreshToken { get => GetValue(KeyRefreshToken, (string?)null); set => SetValue(KeyRefreshToken, value); }

    [MemberNotNullWhen(true, nameof(AccessToken))]
    [MemberNotNullWhen(true, nameof(AccessTokenExpiry))]
    public bool HasValidAccessToken() => !string.IsNullOrWhiteSpace(AccessToken) && AccessTokenExpiry > DateTimeOffset.UtcNow;

    [MemberNotNullWhen(true, nameof(RefreshToken))]
    public bool HasRefreshToken() => !string.IsNullOrWhiteSpace(RefreshToken);

    [return: NotNullIfNotNull(nameof(data))]
    public static implicit operator JsonObject?(ConfigValuesAuthenticationTokens? data) => data?.Inner;

    [return: NotNullIfNotNull(nameof(inner))]
    public static implicit operator ConfigValuesAuthenticationTokens?(JsonObject? inner) => inner is null ? null : new(inner);
}

internal abstract class AbstractConfigValues(JsonObject inner)
{
    protected JsonObject Inner { get; } = inner;

    protected T? GetPrimitiveValue<T>(string key) where T : struct => Inner.TryGetPropertyValue(key, out var node) && node is JsonValue jv && jv.TryGetValue<T>(out var v) ? v : null;
    protected T? GetValue<T>(string key) => Inner.TryGetPropertyValue(key, out var node) && node is JsonValue jv && jv.TryGetValue<T>(out var v) ? v : default;

    protected T GetPrimitiveValue<T>(string key, T defaultValue) where T : struct => GetPrimitiveValue<T>(key) ?? defaultValue;
    protected T GetValue<T>(string key, T defaultValue) => GetValue<T>(key) ?? defaultValue;

    protected JsonObject? GetObject(string key) => Inner.TryGetPropertyValue(key, out var node) && node is JsonObject jo ? jo : null;

    protected void SetValue(string key, JsonNode? node)
    {
        if (node is not null) Inner[key] = node;
        else if (Inner.ContainsKey(key)) Inner.Remove(key);
    }
}
