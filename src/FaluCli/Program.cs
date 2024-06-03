using Azure.Monitor.OpenTelemetry.Exporter;
using Falu;
using Falu.Commands.Config;
using Falu.Commands.Events;
using Falu.Commands.Login;
using Falu.Commands.Messages;
using Falu.Commands.Money;
using Falu.Commands.RequestLogs;
using Falu.Commands.Templates;
using Falu.Config;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.CommandLine.Builder;
using Res = Falu.Properties.Resources;

// Create a root command with some options
var rootCommand = new RootCommand
{
    new LoginCommand(),
    new LogoutCommand(),

    new EventsCommand(),

    new MessagesCommand(),
    new TemplatesCommand(),

    new MoneyCommand(),

    new RequestLogsCommand(),

    new ConfigCommand(),
};

rootCommand.Description = "Official CLI tool for Falu.";
rootCommand.AddGlobalOption(["-v", "--verbose"], "Whether to output verbosely.", false);
rootCommand.AddGlobalOption(["--no-telemetry"], Res.OptionDescriptionNoTelemetry, false);
rootCommand.AddGlobalOption(["--no-updates"], Res.OptionDescriptionNoUpdates, false);

var configValuesLoader = new ConfigValuesLoader();
var configValues = await configValuesLoader.LoadAsync();

var builder = new CommandLineBuilder(rootCommand);

builder.AddMiddleware(async delegate (InvocationContext invocation, Func<InvocationContext, Task> next)
{
    invocation.BindingContext.AddService(_ => configValuesLoader);
    invocation.BindingContext.AddService(_ => configValues);

    var hostBuilder = Host.CreateDefaultBuilder();
    hostBuilder.Properties[typeof(InvocationContext)] = invocation;

    hostBuilder.ConfigureAppConfiguration((context, builder) =>
    {
        var verbose = invocation.IsVerboseEnabled();

        builder.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Logging:LogLevel:Default"] = "Information",
            ["Logging:LogLevel:Microsoft"] = "Warning",
            ["Logging:LogLevel:Microsoft.Hosting.Lifetime"] = "Warning",
            [$"Logging:LogLevel:{typeof(WebsocketHandler).FullName}"] = verbose ? "Trace" : "Information",

            // See https://docs.microsoft.com/en-us/aspnet/core/fundamentals/http-requests?view=aspnetcore-8.0#logging
            ["Logging:LogLevel:System.Net.Http.HttpClient"] = "None", // removes all we do not need
            ["Logging:LogLevel:System.Net.Http.HttpClient.Oidc.ClientHandler"] = verbose ? "Trace" : "Warning", // add what we need
            [$"Logging:LogLevel:System.Net.Http.HttpClient.{nameof(Falu.Client.FaluCliClient)}.ClientHandler"] = verbose ? "Trace" : "Warning", // add what we need
            ["Logging:LogLevel:System.Net.Http.HttpClient.Updates.ClientHandler"] = context.HostingEnvironment.IsDevelopment() && verbose ? "Trace" : "Warning", // add what we need

            ["Logging:Console:FormatterName"] = "Falu",
            ["Logging:Console:FormatterOptions:SingleLine"] = (!verbose).ToString(),
            ["Logging:Console:FormatterOptions:IncludeCategory"] = verbose.ToString(),
            ["Logging:Console:FormatterOptions:IncludeEventId"] = verbose.ToString(),
            ["Logging:Console:FormatterOptions:TimestampFormat"] = "HH:mm:ss ",
        });
    });

    hostBuilder.ConfigureLogging(builder =>
    {
#pragma warning disable IL2026 // Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code
        builder.AddConsoleFormatter<Falu.Logging.FaluConsoleFormatter, Falu.Logging.FaluConsoleFormatterOptions>();
#pragma warning restore IL2026 // Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code
    });

    hostBuilder.ConfigureServices((context, services) =>
    {
        var configuration = context.Configuration;
        var environment = context.HostingEnvironment;

        services.AddSingleton(invocation);
        services.AddSingleton<IHostLifetime, InvocationLifetime>();

        services.AddSingleton(configValuesLoader);
        services.AddFaluClientForCli(configValues);
        services.AddSingleton(configValues);
        services.AddOpenIdProvider(configValues);
        services.AddUpdates(configValues);
        services.AddTransient<WebsocketHandler>();

        var disabled = invocation.IsNoTelemetry() || configValues.NoTelemetry;
        if (!disabled)
        {
            services.AddOpenTelemetry()
                    .ConfigureResource(resource =>
                    {
                        resource.AddAttributes([new("environment", environment.EnvironmentName)]);
                        resource.AddDetector(new OpenTelemetry.ResourceDetectors.Host.HostDetector());
                        resource.AddDetector(new OpenTelemetry.ResourceDetectors.ProcessRuntime.ProcessRuntimeDetector());
                        resource.AddService("falu-cli", serviceVersion: VersioningHelper.ProductVersion);
                    })
                    .WithTracing(tracing =>
                    {
                        tracing.AddSource("System.CommandLine");
                        tracing.AddHttpClientInstrumentation();

                        // add exporter to Azure Monitor
                        var cs = configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"] ?? Constants.AppInsightsConnectionString;
                        tracing.AddAzureMonitorTraceExporter(options => options.ConnectionString = cs);
                    });
        }
    });

    // System.CommandLine library does not create a scope, so we should skip validation of scopes
    hostBuilder.UseDefaultServiceProvider(o => o.ValidateScopes = false);

    var host = hostBuilder.Build();
    try
    {
        invocation.BindingContext.AddService(typeof(IHost), (IServiceProvider _) => host);
        await host.StartAsync();
        await next(invocation);
        await host.StopAsync();
    }
    finally
    {
        host?.Dispose();
    }
});

builder.UseSaveConfigValues()
       .UseActivity();

// we are not using UseDefaults() because we need to supply the exception method
builder.UseVersionOption()
       .UseHelp()
       .UseEnvironmentVariableDirective()
       .UseParseDirective()
       .UseSuggestDirective()
       .RegisterWithDotnetSuggest()
       .UseTypoCorrections()
       .UseParseErrorReporting()
       .UseFaluExceptionHandler()
       .CancelOnProcessTermination();

/* update checker middleware must be added last because it should only run after what the user requested */
builder.UseUpdateChecker();

// Parse the incoming args and invoke the handler
var parser = builder.Build();
return await parser.InvokeAsync(args);
