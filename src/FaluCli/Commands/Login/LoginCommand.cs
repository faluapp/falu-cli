﻿using Falu.Config;
using Falu.Oidc;
using System.Diagnostics;

namespace Falu.Commands.Login;

internal class LoginCommand : FaluCliCommand
{
    public LoginCommand() : base("login", "Login to your Falu account to setup the CLI")
    {
        this.AddOption(["--no-browser"],
                       description: "Set true to not open the browser automatically for authentication.",
                       defaultValue: false);

        //this.AddOption(["-i", "--interactive"],
        //               description: "Run interactive configuration mode if you cannot open a browser.",
        //               defaultValue: false);
    }

    public override async Task<int> ExecuteAsync(CliCommandExecutionContext context, CancellationToken cancellationToken)
    {
        var oidcProvider = context.GetRequiredService<OidcProvider>();

        var noBrowser = context.ParseResult.ValueForOption<bool>("--no-browser");

        // perform device authorization
        context.Logger.LogInformation("Performing device authentication. You will be redirected to the browser.");

        var authResp = await oidcProvider.RequestDeviceAuthorizationAsync(cancellationToken);
        if (authResp.IsError) throw new LoginException(authResp);

        // inform the user where to authentication
        context.Logger.LogInformation("To authenticate, open your web browser at {VerificationUri} and enter the code {UserCode}.", authResp.VerificationUri, authResp.UserCode);

        // open browser unless told not to
        if (!noBrowser && authResp.VerificationUriComplete is not null)
        {
            context.Logger.LogInformation("Automatically opening the browser ...");

            // delay for 2 seconds before opening the browser for the user to see the code
            await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
            Process.Start(new ProcessStartInfo(authResp.VerificationUriComplete) { UseShellExecute = true });
        }

        // get the token via polling
        var interval = authResp.Interval ?? 5;
        OidcTokenResponse tokenResp;
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            tokenResp = await oidcProvider.RequestDeviceTokenAsync(authResp.DeviceCode, cancellationToken);

            if (tokenResp.IsError)
            {
                var msg = tokenResp.Error switch
                {
                    "authorization_pending" => "Authorization is pending",
                    "slow_down" => "Slowing down check for authorization.",
                    _ => throw new LoginException(tokenResp),
                };

                // when error is "slow_down" the interval MUST be increased by 5 seconds for this and all subsequent requests
                if (tokenResp.Error == "slow_down") interval += 5;

                context.Logger.LogInformation("{Message} Delaying for {Duration} seconds", msg, interval);
                await Task.Delay(TimeSpan.FromSeconds(interval), cancellationToken);
            }
            else
            {
                break;
            }
        }

        context.Logger.LogInformation("Authentication tokens issued successfully.");

        // set the authentication information
        context.ConfigValues.Authentication = new ConfigValuesAuthenticationTokens(tokenResp);

        return 0;
    }
}
