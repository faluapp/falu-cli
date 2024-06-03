using Falu.Oidc;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Falu.Config;

internal sealed class ConfigValues(JsonObject inner) : AbstractConfigValues(inner)
{
    private const string KeyNoTelemetry = "no_telemetry";
    private const string KeyNoUpdates = "no_updates";
    private const string KeyLastUpdateCheck = "last_update_check";
    private const string KeyRetries = "retries";
    private const string KeyTimeout = "timeout";
    private const string KeyDefaultWorkspaceId = "default_workspace_id";
    private const string KeyDefaultLiveMode = "default_live_mode";
    private const string KeyAuthentication = "authentication";

    public bool NoTelemetry { get => GetValue(KeyNoTelemetry, false); set => SetValue(KeyNoTelemetry, value); }
    public bool NoUpdates { get => GetValue(KeyNoUpdates, false); set => SetValue(KeyNoUpdates, value); }
    public DateTimeOffset? LastUpdateCheck { get => GetValue(KeyLastUpdateCheck, (DateTimeOffset?)null); set => SetValue(KeyLastUpdateCheck, value); }
    public int Retries { get => GetValue(KeyRetries, 0); set => SetValue(KeyRetries, value); }
    public int Timeout { get => GetValue(KeyTimeout, 120); set => SetValue(KeyTimeout, value); }
    public string? DefaultWorkspaceId { get => GetValue(KeyDefaultWorkspaceId, (string?)null); set => SetValue(KeyDefaultWorkspaceId, value); }
    public bool DefaultLiveMode { get => GetValue(KeyDefaultLiveMode, false); set => SetValue(KeyDefaultLiveMode, value); }

    public AuthenticationTokenConfigData? Authentication
    {
        get => GetObject(KeyAuthentication);
        set => SetValue(KeyAuthentication, value);
    }

    public string Hash() => Convert.ToBase64String(System.Security.Cryptography.MD5.HashData(System.Text.Encoding.UTF8.GetBytes(Inner.ToJsonString())));
    public string Json(JsonSerializerOptions serializerOptions) => Inner.ToJsonString(serializerOptions);
}

internal sealed class AuthenticationTokenConfigData : AbstractConfigValues
{
    private const string KeyAccessToken = "access_token";
    private const string KeyAccessTokenExpiry = "access_token_expiry";
    private const string KeyRefreshToken = "refresh_token";

    public AuthenticationTokenConfigData(JsonObject inner) : base(inner) { }

    public AuthenticationTokenConfigData(OidcTokenResponse response) : base([])
    {
        AccessToken = response.AccessToken;
        RefreshToken = response.RefreshToken;
        AccessTokenExpiry = DateTimeOffset.UtcNow.AddSeconds(response.ExpiresIn).AddSeconds(-5);
    }

    public string? AccessToken { get => GetValue(KeyAccessToken, (string?)null); set => SetValue(KeyAccessToken, value); }
    public DateTimeOffset? AccessTokenExpiry { get => GetValue(KeyAccessTokenExpiry, (DateTimeOffset?)null); set => SetValue(KeyAccessTokenExpiry, value); }
    public string? RefreshToken { get => GetValue(KeyRefreshToken, (string?)null); set => SetValue(KeyRefreshToken, value); }

    [MemberNotNullWhen(true, nameof(AccessToken))]
    [MemberNotNullWhen(true, nameof(AccessTokenExpiry))]
    public bool HasValidAccessToken() => !string.IsNullOrWhiteSpace(AccessToken) && AccessTokenExpiry > DateTimeOffset.UtcNow;

    [MemberNotNullWhen(true, nameof(RefreshToken))]
    public bool HasRefreshToken() => !string.IsNullOrWhiteSpace(RefreshToken);

    [return: NotNullIfNotNull(nameof(data))]
    public static implicit operator JsonObject?(AuthenticationTokenConfigData? data) => data?.Inner;

    [return: NotNullIfNotNull(nameof(inner))]
    public static implicit operator AuthenticationTokenConfigData?(JsonObject? inner) => inner is null ? null : new(inner);
}

internal abstract class AbstractConfigValues(JsonObject inner)
{
    protected JsonObject Inner { get; } = inner;

    protected T GetValue<T>(string key, T defaultValue)
    {
        return Inner.TryGetPropertyValue(key, out var node) && node is JsonValue value && value.TryGetValue<T>(out var result)
            ? result
            : defaultValue;
    }

    protected JsonObject? GetObject(string key)
    {
        return Inner.TryGetPropertyValue(key, out var node) && node is JsonObject result
            ? result
            : default;
    }

    protected void SetValue(string key, JsonNode? node)
    {
        if (node is not null) Inner[key] = node;
        else if (Inner.ContainsKey(key)) Inner.Remove(key);
    }
}
