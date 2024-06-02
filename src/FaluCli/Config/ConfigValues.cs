using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Falu.Config;

internal record ConfigValues
{
    [JsonPropertyName("skip_update_checks")]
    public bool SkipUpdateChecks { get; set; }

    [JsonPropertyName("no_telemetry")]
    public bool NoTelemetry { get; set; }

    [JsonPropertyName("retries")]
    public int Retries { get; set; } = 0;

    [JsonPropertyName("timeout")]
    public int Timeout { get; set; } = 120;

    [JsonPropertyName("default_workspace_id")]
    public string? DefaultWorkspaceId { get; set; }

    [JsonPropertyName("default_live_mode")]
    public bool DefaultLiveMode { get; set; }

    [JsonPropertyName("authentication")]
    public AuthenticationTokenConfigData? Authentication { get; set; }
}

internal record AuthenticationTokenConfigData
{
    [JsonPropertyName("access_token")]
    public string? AccessToken { get; set; }

    [JsonPropertyName("access_token_expiry")]
    public DateTimeOffset? AccessTokenExpiry { get; set; }

    [JsonPropertyName("refresh_token")]
    public string? RefreshToken { get; set; }

    [MemberNotNullWhen(true, nameof(AccessToken))]
    [MemberNotNullWhen(true, nameof(AccessTokenExpiry))]
    public bool HasValidAccessToken() => !string.IsNullOrWhiteSpace(AccessToken) && AccessTokenExpiry > DateTimeOffset.UtcNow;

    [MemberNotNullWhen(true, nameof(RefreshToken))]
    public bool HasValidRefreshToken() => !string.IsNullOrWhiteSpace(RefreshToken);
}
