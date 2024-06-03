using Falu.Config;
using Falu.Oidc;
using System.Net.Http.Headers;
using Res = Falu.Properties.Resources;

namespace Falu.Client;

internal class FaluCliClientHandler(OidcProvider oidcProvider,
                                    InvocationContext context,
                                    ILogger<FaluCliClientHandler> logger) : DelegatingHandler
{
    /// <inheritdoc/>
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // Override the X-Idempotency-Key header if CLI contains the --idempotency-key option
        var idempotencyKey = context.ParseResult.ValueForOption<string>("--idempotency-key");
        if (!string.IsNullOrWhiteSpace(idempotencyKey))
        {
            request.Headers.Replace("X-Idempotency-Key", idempotencyKey);
        }

        // if we do not have a key, we use the user credentials in the configuration
        var key = context.ParseResult.ValueForOption<string>("--apikey");
        if (string.IsNullOrWhiteSpace(key))
        {
            // (1) Override the X-Workspace-Id header if CLI contains the option
            if (context.TryGetWorkspaceId(out var workspaceId))
            {
                request.Headers.Replace("X-Workspace-Id", workspaceId);
            }

            // (2) Override the X-Live-Mode header if CLI contains the option
            if (context.TryGetLiveMode(out var live))
            {
                request.Headers.Replace("X-Live-Mode", live.Value.ToString().ToLowerInvariant());
            }

            // (3) Handle appropriate authentication

            // ensure we have login information and that it contains a valid access token or refresh token
            var config = context.GetConfigValues();
            if (config.Authentication is null || (!config.Authentication.HasValidAccessToken() && !config.Authentication.HasValidRefreshToken()))
            {
                throw new FaluException(Res.AuthenticationInformationMissing);
            }

            // at this point, we either have a valid access token or an invalid access token with a valid refresh token
            // if the access token is invalid, we need to get one via the refresh token
            if (!config.Authentication.HasValidAccessToken() && config.Authentication.HasValidRefreshToken())
            {
                logger.LogInformation("Requesting for a new access token using the saved refresh token");

                // request for a new token using the refresh token
                var token_resp = await oidcProvider.RequestRefreshTokenAsync(config.Authentication.RefreshToken, cancellationToken);
                if (token_resp.IsError)
                {
                    throw new FaluException(Res.RefreshingAccessTokenFailed);
                }

                logger.LogInformation("Access token refreshed.");
            }

            key = config.Authentication.AccessToken;
        }

        // at this point we have a key and we can proceed to set the authentication header
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key);

        // (5) Execute the modified request
        return await base.SendAsync(request, cancellationToken);
    }
}
