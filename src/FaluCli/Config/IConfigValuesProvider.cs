using Falu.Oidc;

namespace Falu.Config;

internal interface IConfigValuesProvider
{
    Task<ConfigValues> GetConfigValuesAsync(CancellationToken cancellationToken = default);
    Task SaveConfigValuesAsync(CancellationToken cancellationToken = default);
    Task SaveConfigValuesAsync(OidcTokenResponse response, CancellationToken cancellationToken = default);

    Task ClearAuthenticationAsync(CancellationToken cancellationToken = default);
    void ClearAll();
}
