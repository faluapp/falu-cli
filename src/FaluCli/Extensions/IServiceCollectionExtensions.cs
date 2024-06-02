using Falu;
using Falu.Client;
using Falu.Config;
using Falu.Oidc;
using System.Net.Http.Headers;

namespace Microsoft.Extensions.DependencyInjection;

internal static class IServiceCollectionExtensions
{
    // services are registered as transit to allow for easier debugging because no scope is created by the parser

    public static IServiceCollection AddFaluClientForCli(this IServiceCollection services, ConfigValues configValues)
    {
        var builder = services.AddFalu<FaluCliClient, FaluClientOptions>(options =>
        {
            // A dummy ApiKey is used so that the options validator can pass
            options.ApiKey = "dummy";
            options.Retries = configValues.Retries;
        });

        builder.AddHttpMessageHandler<FaluCliClientHandler>()
               .ConfigureHttpClientStandard(configValues);

        services.AddTransient<FaluCliClientHandler>();

        return services;
    }

    public static IServiceCollection AddOpenIdProvider(this IServiceCollection services, ConfigValues configValues)
    {
        services.AddHttpClient<OidcProvider>(name: "Oidc")
                .ConfigureHttpClientStandard(configValues, (_, client) =>
                {
                    // only JSON responses
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                });

        return services;
    }

    public static IServiceCollection AddUpdates(this IServiceCollection services, ConfigValues configValues)
    {
        services.AddHttpClient(name: "Updates")
                .ConfigureHttpClientStandard(configValues);

        return services;
    }

    private static IHttpClientBuilder ConfigureHttpClientStandard(this IHttpClientBuilder builder, ConfigValues configValues, Action<IServiceProvider, HttpClient>? configure = null)
    {
        return builder.ConfigureHttpClient((provider, client) =>
        {
            // change the User-Agent header
            client.DefaultRequestHeaders.UserAgent.Clear();
            client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("falucli", VersioningHelper.ProductVersion));

            // set the Timeout from ConfigValues
            client.Timeout = TimeSpan.FromSeconds(configValues.Timeout);

            // continue the configuration
            configure?.Invoke(provider, client);
        });
    }
}
