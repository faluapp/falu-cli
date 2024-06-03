using Microsoft.Extensions.Hosting.Internal;
using Microsoft.Extensions.Options;

namespace Falu;

internal class InvocationLifetime(IOptions<ConsoleLifetimeOptions> options,
                                  IHostEnvironment environment,
                                  IHostApplicationLifetime applicationLifetime,
                                  IOptions<HostOptions> hostOptions,
                                  ILoggerFactory loggerFactory)
    : ConsoleLifetime(options, environment, applicationLifetime, hostOptions, loggerFactory), IDisposable
{
    private readonly IHostApplicationLifetime applicationLifetime = applicationLifetime;
    private CancellationTokenRegistration reg;

    public new async Task WaitForStartAsync(CancellationToken cancellationToken)
    {
        await base.WaitForStartAsync(cancellationToken);

        // The token comes from HostingAction.InvokeAsync
        // and it's the invocation cancellation token.
        reg = cancellationToken.Register(state =>
        {
            ((InvocationLifetime)state!).applicationLifetime.StopApplication();
        }, this);
    }

    public new void Dispose()
    {
        reg.Dispose();
        base.Dispose();
    }
}
