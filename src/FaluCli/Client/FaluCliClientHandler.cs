using Falu.Config;
using Falu.Oidc;
using System.Net.Http.Headers;
using Res = Falu.Properties.Resources;

namespace Falu.Client;

internal class FaluCliClientHandler(ConfigValues configValues, ParseResult parseResult, OidcProvider oidcProvider, ILogger<FaluCliClientHandler> logger) : DelegatingHandler
{
    /// <inheritdoc/>
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var command = (FaluCliCommand)parseResult.CommandResult.Command;

        // Override the X-Idempotency-Key header if CLI contains the --idempotency-key option
        var idempotencyKey = command.GetIdempotencyKey(parseResult);
        if (!string.IsNullOrWhiteSpace(idempotencyKey))
        {
            request.Headers.Replace("X-Idempotency-Key", idempotencyKey);
        }

        // if we do not have a key, we use the user credentials in the configuration
        var key = command.GetApiKey(parseResult);
        if (string.IsNullOrWhiteSpace(key))
        {
            // (1) Set the X-Workspace-Id and X-Live-Mode headers but skip for /workspaces
            if (!request.RequestUri!.ToString().Contains("/workspaces"))
            {
                // (1a) Set the X-Workspace-Id header using the CLI option to override the default
                if (command.TryGetWorkspace(parseResult, out var workspaceId))
                {
                    // check if the workspace exists in the configuration
                    workspaceId = configValues.GetRequiredWorkspace(workspaceId).Id;
                }
                workspaceId ??= (workspaceId ?? configValues.DefaultWorkspaceId) ?? throw new FaluException(Res.MissingWorkspaceId);
                request.Headers.Replace("X-Workspace-Id", workspaceId);

                // (1b) Set the X-Live-Mode header using CLI option to override the default
                var live = command.GetLiveMode(parseResult) ?? configValues.DefaultLiveMode;
                if (live is not null)
                {
                    request.Headers.Replace("X-Live-Mode", live.Value.ToString().ToLowerInvariant()); // when absent, the server assumes false
                }
            }

            // (2) Handle appropriate authentication

            // ensure we have login information and that it contains a valid access token or refresh token
            if (configValues.Authentication is null || (!configValues.Authentication.HasValidAccessToken() && !configValues.Authentication.HasRefreshToken()))
            {
                throw new FaluException(Res.AuthenticationInformationMissing);
            }

            // at this point, we either have a valid access token or an invalid access token with a valid refresh token
            // if the access token is invalid, we need to get one via the refresh token
            if (!configValues.Authentication.HasValidAccessToken() && configValues.Authentication.HasRefreshToken())
            {
                logger.LogInformation("Requesting for a new access token using the saved refresh token");

                // request for a new token using the refresh token
                var token_resp = await oidcProvider.RequestRefreshTokenAsync(configValues.Authentication.RefreshToken, cancellationToken);
                if (token_resp.IsError)
                {
                    throw new FaluException(Res.RefreshingAccessTokenFailed);
                }

                configValues.Authentication = new ConfigValuesAuthenticationTokens(token_resp);
                logger.LogInformation("Access token refreshed.");
            }

            key = configValues.Authentication.AccessToken;
        }

        // at this point we have a key and we can proceed to set the authentication header
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key);

        // (3) Execute the modified request
        return await base.SendAsync(request, cancellationToken);
    }
}
