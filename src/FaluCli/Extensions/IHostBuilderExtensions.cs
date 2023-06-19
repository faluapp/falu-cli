using System.CommandLine.Hosting;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Extensions.Hosting;

internal static class IHostBuilderExtensions
{
    // this exists to work around trimming which does not keep constructors by default in the System.CommandLine.Hosting library
    // though Microsoft.Extensions.DependencyInjection does
    public static IHostBuilder UseCommandHandlerTrimmable<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TCommand,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] THandler>(this IHostBuilder builder)
        where TCommand : Command
        where THandler : ICommandHandler
    {
        return builder.UseCommandHandler<TCommand, THandler>();
    }
}
