using Falu;
using Falu.Config;
using System.Net.Http.Headers;

namespace Microsoft.Extensions.DependencyInjection;

internal static class IHttpClientBuilderExtensions
{
    public static IHttpClientBuilder ConfigureHttpClientStandard(this IHttpClientBuilder builder, ConfigValues configValues, Action<IServiceProvider, HttpClient>? configure = null)
    {
        return builder.ConfigureHttpClient((provider, client) =>
        {
            // change the User-Agent header
            client.DefaultRequestHeaders.UserAgent.Clear();
            client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue(Constants.ProductName, Constants.ProductVersion));

            // set the Timeout from ConfigValues
            client.Timeout = TimeSpan.FromSeconds(configValues.Timeout);

            // continue the configuration
            configure?.Invoke(provider, client);
        });
    }
}
