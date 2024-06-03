using Microsoft.Extensions.Hosting.Internal;
using Microsoft.Extensions.Options;

namespace Falu;

internal class InvocationLifetime(IOptions<ConsoleLifetimeOptions> options,
                                  IHostEnvironment environment,
                                  IHostApplicationLifetime applicationLifetime,
                                  IOptions<HostOptions> hostOptions,
                                  ILoggerFactory loggerFactory)
    : ConsoleLifetime(options, environment, applicationLifetime, hostOptions, loggerFactory), IHostLifetime, IDisposable
{
    private readonly IHostApplicationLifetime applicationLifetime = applicationLifetime;
    private CancellationTokenRegistration reg;

    async Task IHostLifetime.WaitForStartAsync(CancellationToken cancellationToken)
    {
        await ((ConsoleLifetime)(object)this).WaitForStartAsync(cancellationToken);

        // The token comes from FaluRootCliAction.InvokeAsync
        // and it's the invocation cancellation token.
        reg = cancellationToken.Register(state =>
        {
            ((InvocationLifetime)state!).applicationLifetime.StopApplication();
        }, this);
    }

    async Task IHostLifetime.StopAsync(CancellationToken cancellationToken)
    {
        await ((ConsoleLifetime)(object)this).StopAsync(cancellationToken);
    }

    void IDisposable.Dispose()
    {
        reg.Dispose();
        ((ConsoleLifetime)(object)this).Dispose();
    }
}
