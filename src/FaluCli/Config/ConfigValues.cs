using System.Diagnostics.CodeAnalysis;

namespace Falu.Config;

internal record ConfigValues
{
    public int Retries { get; set; } = 0;
    public int Timeout { get; set; } = 120;
    public string? DefaultWorkspaceId { get; set; }
    public bool DefaultLiveMode { get; set; }
    public AuthenticationTokenConfigData? Authentication { get; set; }
}

internal record AuthenticationTokenConfigData
{
    public string? AccessToken { get; set; }
    public DateTimeOffset? AccessTokenExpiry { get; set; }
    public string? RefreshToken { get; set; }

    [MemberNotNullWhen(true, nameof(AccessToken))]
    [MemberNotNullWhen(true, nameof(AccessTokenExpiry))]
    public bool HasValidAccessToken() => !string.IsNullOrWhiteSpace(AccessToken) && AccessTokenExpiry > DateTimeOffset.UtcNow;

    [MemberNotNullWhen(true, nameof(RefreshToken))]
    public bool HasValidRefreshToken() => !string.IsNullOrWhiteSpace(RefreshToken);
}
