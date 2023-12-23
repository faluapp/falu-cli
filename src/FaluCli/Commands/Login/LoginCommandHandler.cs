using Falu.Config;
using Falu.Oidc;
using System.Diagnostics;

namespace Falu.Commands.Login;

internal class LoginCommandHandler(OidcProvider oidcProvider, IConfigValuesProvider configValuesProvider, ILogger<LoginCommandHandler> logger) : ICommandHandler
{
    int ICommandHandler.Invoke(InvocationContext context) => throw new NotImplementedException();

    public async Task<int> InvokeAsync(InvocationContext context)
    {
        var cancellationToken = context.GetCancellationToken();
        var noBrowser = context.ParseResult.ValueForOption<bool>("--no-browser");

        // perform device authorization
        var auth_resp = await RequestAuthorizationAsync(noBrowser, cancellationToken);

        // get the token via polling
        var token_resp = await RequestTokenAsync(auth_resp, cancellationToken);
        logger.LogInformation("Authentication tokens issued successfully.");

        // save the authentication information
        await configValuesProvider.SaveConfigValuesAsync(token_resp, cancellationToken);

        return 0;
    }

    private async Task<OidcDeviceAuthorizationResponse> RequestAuthorizationAsync(bool noBrowser, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Performing device authentication. You will be redirected to the browser.");

        var response = await oidcProvider.RequestDeviceAuthorizationAsync(cancellationToken);
        if (response.IsError) throw new LoginException(response);

        // inform the user where to authentication
        logger.LogInformation("To authenticate, open your web browser at {VerificationUri} and enter the code {UserCode}.",
                              response.VerificationUri,
                              response.UserCode);

        // open browser unless told not to
        if (!noBrowser && response.VerificationUriComplete is not null)
        {
            logger.LogInformation("Automatically opening the browser ...");

            // delay for 2 seconds before opening the browser for the user to see the code
            await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
            Process.Start(new ProcessStartInfo(response.VerificationUriComplete) { UseShellExecute = true });
        }

        return response;
    }

    private async Task<OidcTokenResponse> RequestTokenAsync(OidcDeviceAuthorizationResponse auth, CancellationToken cancellationToken = default)
    {
        var interval = auth.Interval ?? 5;
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var response = await oidcProvider.RequestDeviceTokenAsync(auth.DeviceCode, cancellationToken);

            if (response.IsError)
            {
                var msg = response.Error switch
                {
                    "authorization_pending" => "Authorization is pending",
                    "slow_down" => "Slowing down check for authorization.",
                    _ => throw new LoginException(response),
                };

                // when error is "slow_down" the interval MUST be increased by 5 seconds for this and all subsequent requests
                if (response.Error == "slow_down") interval += 5;

                logger.LogInformation("{Message} Delaying for {Duration} seconds", msg, interval);
                await Task.Delay(TimeSpan.FromSeconds(interval), cancellationToken);
            }
            else
            {
                return response;
            }
        }
    }
}
