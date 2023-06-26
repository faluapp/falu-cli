namespace Falu.Oidc;

public interface IOidcProvider
{
    Task<OidcTokenResponse> RequestRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);
    Task<OidcDeviceAuthorizationResponse> RequestDeviceAuthorizationAsync(CancellationToken cancellationToken = default);
    Task<OidcTokenResponse> RequestDeviceTokenAsync(string deviceCode, CancellationToken cancellationToken = default);
}
