using Azure.Monitor.OpenTelemetry.Exporter;
using Falu;
using Falu.Config;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.CommandLine.Hosting;

namespace Microsoft.Extensions.Hosting;

internal static class IHostBuilderExtensions
{
    // this value is hardcoded because Microsoft does not consider the instrumentation key sensitive
    private const string AppInsightsConnectionString = "InstrumentationKey=05728099-c2aa-411d-8f1c-e3aa9689daae;IngestionEndpoint=https://westeurope-5.in.applicationinsights.azure.com/;LiveEndpoint=https://westeurope.livediagnostics.monitor.azure.com/;ApplicationId=bb8bb015-675d-4658-a286-5d2108ca437a";

    public static IHostBuilder AddOpenTelemetry(this IHostBuilder builder, ConfigValues configValues)
    {
        var context = builder.GetInvocationContext();
        var disabled = context.IsNoTelemetry() || configValues.NoTelemetry;
        if (disabled) return builder;

        builder.ConfigureServices((context, services) =>
        {
            var environment = context.HostingEnvironment;
            var configuration = context.Configuration;
            var builder = services.AddOpenTelemetry();

            // configure the resource
            builder.ConfigureResource(builder =>
            {
                builder.AddAttributes([new("environment", environment.EnvironmentName)]);

                // add detectors
                builder.AddDetector(new OpenTelemetry.ResourceDetectors.Host.HostDetector());
                builder.AddDetector(new OpenTelemetry.ResourceDetectors.ProcessRuntime.ProcessRuntimeDetector());

                // add service name and version (should override any existing values)
                builder.AddService("falu-cli", serviceVersion: VersioningHelper.ProductVersion);
            });

            // add tracing
            builder.WithTracing(tracing =>
            {
                tracing.AddSource("System.CommandLine");
                tracing.AddHttpClientInstrumentation();

                // add exporter to Azure Monitor
                var aics = configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"] ?? AppInsightsConnectionString;
                tracing.AddAzureMonitorTraceExporter(options => options.ConnectionString = aics);
            });
        });

        return builder;
    }
}
