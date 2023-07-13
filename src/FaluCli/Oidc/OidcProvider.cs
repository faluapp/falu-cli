using System.Net.Http.Json;
using System.Text.Json.Serialization.Metadata;

namespace Falu.Oidc;

internal class OidcProvider
{
    private readonly HttpClient httpClient;

    public OidcProvider(HttpClient httpClient)
    {
        this.httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    public Task<OidcTokenResponse> RequestRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var parameters = new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token",
            ["client_id"] = Constants.ClientId,
            ["refresh_token"] = refreshToken,
        };

        return SendAsync(Constants.TokenEndpoint, parameters, FaluCliJsonSerializerContext.Default.OidcTokenResponse, cancellationToken);
    }

    public Task<OidcDeviceAuthorizationResponse> RequestDeviceAuthorizationAsync(CancellationToken cancellationToken = default)
    {
        var parameters = new Dictionary<string, string>
        {
            ["client_id"] = Constants.ClientId,
            ["scope"] = Constants.Scopes,
        };

        return SendAsync(Constants.DeviceAuthorizationEndpoint, parameters, FaluCliJsonSerializerContext.Default.OidcDeviceAuthorizationResponse, cancellationToken);
    }

    public Task<OidcTokenResponse> RequestDeviceTokenAsync(string deviceCode, CancellationToken cancellationToken = default)
    {
        var parameters = new Dictionary<string, string>
        {
            ["grant_type"] = "urn:ietf:params:oauth:grant-type:device_code",
            ["client_id"] = Constants.ClientId,
            ["device_code"] = deviceCode,
        };

        return SendAsync(Constants.TokenEndpoint, parameters, FaluCliJsonSerializerContext.Default.OidcTokenResponse, cancellationToken);
    }

    private async Task<T> SendAsync<T>(string requestUri,
                                       Dictionary<string, string> parameters,
                                       JsonTypeInfo<T> jsonTypeInfo,
                                       CancellationToken cancellationToken = default)
    {
        var content = new FormUrlEncodedContent(parameters);
        var request = new HttpRequestMessage(HttpMethod.Post, requestUri) { Content = content, };
        var response = await httpClient.SendAsync(request, cancellationToken);
        return (await response.Content.ReadFromJsonAsync(jsonTypeInfo, cancellationToken))!;
    }
}
