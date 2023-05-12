using Falu.Config;
using IdentityModel;
using IdentityModel.Client;
using System.Diagnostics;

namespace Falu.Commands.Login;

internal class LoginCommandHandler : ICommandHandler
{
    private readonly HttpClient client;
    private readonly IConfigValuesProvider configValuesProvider;
    private readonly ILogger logger;
    private readonly IDiscoveryCache discoveryCache;

    public LoginCommandHandler(IHttpClientFactory httpClientFactory, IDiscoveryCache discoveryCache, IConfigValuesProvider configValuesProvider, ILogger<LoginCommandHandler> logger)
    {
        client = httpClientFactory?.CreateOpenIdClient() ?? throw new ArgumentNullException(nameof(httpClientFactory));
        this.discoveryCache = discoveryCache ?? throw new ArgumentNullException(nameof(discoveryCache));
        this.configValuesProvider = configValuesProvider ?? throw new ArgumentNullException(nameof(configValuesProvider));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    int ICommandHandler.Invoke(InvocationContext context) => throw new NotImplementedException();

    public async Task<int> InvokeAsync(InvocationContext context)
    {
        var cancellationToken = context.GetCancellationToken();
        var noBrowser = context.ParseResult.ValueForOption<bool>("--no-browser");

        // perform confirguration discovery
        var disco = await discoveryCache.GetSafelyAsync(cancellationToken);

        // perform device authorization
        var auth_resp = await RequestAuthorizationAsync(disco, noBrowser, cancellationToken);

        // get the token via polling
        var token_resp = await RequestTokenAsync(disco, auth_resp, cancellationToken);
        logger.LogInformation("Authentication tokens issued successfully.");

        // save the authentication information
        await configValuesProvider.SaveConfigValuesAsync(token_resp, cancellationToken);

        return 0;
    }

    private async Task<DeviceAuthorizationResponse> RequestAuthorizationAsync(DiscoveryDocumentResponse disco, bool noBrowser, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Performing device authentication. You will be redirected to the browser.");

        var request = new DeviceAuthorizationRequest
        {
            Address = disco.DeviceAuthorizationEndpoint,
            ClientId = Constants.ClientId,
            ClientCredentialStyle = ClientCredentialStyle.PostBody,
            Scope = Constants.Scopes,
        };
        var response = await client.RequestDeviceAuthorizationAsync(request, cancellationToken);
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

    private async Task<TokenResponse> RequestTokenAsync(DiscoveryDocumentResponse disco, DeviceAuthorizationResponse auth, CancellationToken cancellationToken = default)
    {
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var request = new DeviceTokenRequest
            {
                Address = disco.TokenEndpoint,
                ClientId = Constants.ClientId,
                DeviceCode = auth.DeviceCode ?? throw new InvalidOperationException("Device code in the response cannot be null. Contact support!"),
            };
            var response = await client.RequestDeviceTokenAsync(request, cancellationToken);

            if (response.IsError)
            {
                var msg = response.Error switch
                {
                    OidcConstants.TokenErrors.AuthorizationPending => "Authorization is pending",
                    OidcConstants.TokenErrors.SlowDown => "Slowing down check for authorization.",
                    _ => throw new LoginException(response),
                };

                logger.LogInformation("{Message} Delaying for {Duration} seconds", msg, auth.Interval);
                await Task.Delay(TimeSpan.FromSeconds(auth.Interval), cancellationToken);
            }
            else
            {
                return response;
            }
        }
    }
}
