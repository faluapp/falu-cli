using Falu.Config;
using Microsoft.Extensions.Options;

namespace Falu.Client;

internal class FaluClientConfigureOptions(IConfigValuesProvider configValuesProvider) : IConfigureOptions<FaluClientOptions>
{
    public void Configure(FaluClientOptions options)
    {
        var values = configValuesProvider.GetConfigValuesAsync().GetAwaiter().GetResult();
        options.Retries = values.Retries;
    }
}
