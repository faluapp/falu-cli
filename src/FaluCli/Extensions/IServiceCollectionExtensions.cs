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
               .ConfigureHttpClientStandard();

        services.AddTransient<FaluCliClientHandler>();

        return services;
    }

    public static IServiceCollection AddOpenIdProvider(this IServiceCollection services)
    {
        services.AddHttpClient<OidcProvider>(name: "Oidc")
                .ConfigureHttpClientStandard((_, client) =>
                {
                    // only JSON responses
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                });

        return services;
    }

    public static IServiceCollection AddUpdates(this IServiceCollection services)
    {
        services.AddHttpClient(name: "Updates")
                .ConfigureHttpClientStandard();

        return services;
    }

    private static IHttpClientBuilder ConfigureHttpClientStandard(this IHttpClientBuilder builder, Action<IServiceProvider, HttpClient>? configure = null)
    {
        return builder.ConfigureHttpClient((provider, client) =>
        {
            // change the User-Agent header
            client.DefaultRequestHeaders.UserAgent.Clear();
            client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("falucli", VersioningHelper.ProductVersion));

            // set the Timeout from ConfigValues
            var configValuesProvider = provider.GetRequiredService<IConfigValuesProvider>();
            var configValues = configValuesProvider.GetConfigValuesAsync().GetAwaiter().GetResult();
            client.Timeout = TimeSpan.FromSeconds(configValues.Timeout);

            // continue the configuration
            configure?.Invoke(provider, client);
        });
    }
}
