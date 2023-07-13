using Falu;
using Falu.Client;
using Falu.Config;
using Falu.Oidc;
using Falu.Updates;
using System.Net.Http.Headers;

namespace Microsoft.Extensions.DependencyInjection;

internal static class IServiceCollectionExtensions
{
    // services are registered as transit to allow for easier debugging because no scope is created by the parser

    public static IServiceCollection AddFaluClientForCli(this IServiceCollection services)
    {
        // A dummy ApiKey is used so that the options validator can pass
        services.AddFalu<FaluCliClient, FaluClientOptions>(o => o.ApiKey = "dummy")
                .AddHttpMessageHandler<FaluCliClientHandler>()
                .ConfigureHttpClientStandard();

        services.AddTransient<FaluCliClientHandler>();
        services.ConfigureOptions<FaluClientConfigureOptions>();

        return services;
    }

    public static IServiceCollection AddUpdateChecker(this IServiceCollection services)
    {
        services.AddHttpClient<UpdateChecker>()
                .ConfigureHttpClientStandard();

        return services.AddSingleton<IHostedService>(p => p.GetRequiredService<UpdateChecker>());
    }

    public static IServiceCollection AddConfigValuesProvider(this IServiceCollection services)
    {
        return services.AddTransient<IConfigValuesProvider, ConfigValuesProvider>();
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
