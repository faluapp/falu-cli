using Azure.Monitor.OpenTelemetry.Exporter;
using Falu;
using Falu.Client;
using Falu.Commands;
using Falu.Commands.Events;
using Falu.Commands.Messages;
using Falu.Commands.Money;
using Falu.Commands.RequestLogs;
using Falu.Commands.Templates;
using Falu.Config;
using Falu.Oidc;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

// create a root command
var rootCommand = new CliRootCommand(description: "Official CLI tool for Falu.")
{
    new LoginCommand(),
    new LogoutCommand(),

    new EventsCommand(),

    new MessagesCommand(),
    new TemplatesCommand(),

    new MoneyCommand(),

    new RequestLogsCommand(),

    new WorkspacesCommand(),
    new ConfigCommand(),
};

// create the configuration
var configuration = new CliConfiguration(rootCommand)
{
    EnableDefaultExceptionHandler = false, // we have out own exception handling
    //EnableParseErrorReporting = true,
    //EnableTypoCorrections = true,
};

// parse the arguments
var parseResult = configuration.Parse(args);
var command = parseResult.CommandResult.Command as FaluCliCommand;

// load the configuration values
var configValuesLoader = new ConfigValuesLoader();
var configValues = await configValuesLoader.LoadAsync();

// create the host
var builder = Host.CreateApplicationBuilder();

// configure app configuration
var verbose = command?.IsVerboseEnabled(parseResult) ?? false;
builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
{
    ["Logging:LogLevel:Default"] = "Information",
    ["Logging:LogLevel:Microsoft"] = "Warning",
    ["Logging:LogLevel:Microsoft.Hosting.Lifetime"] = "Warning",
    [$"Logging:LogLevel:{typeof(WebsocketHandler).FullName}"] = verbose ? "Trace" : "Information",
    [$"Logging:LogLevel:{parseResult.CommandResult.Command.GetType().FullName}"] = verbose ? "Trace" : "Information",

    // See https://docs.microsoft.com/en-us/aspnet/core/fundamentals/http-requests?view=aspnetcore-8.0#logging
    ["Logging:LogLevel:System.Net.Http.HttpClient"] = "None", // removes all we do not need
    ["Logging:LogLevel:System.Net.Http.HttpClient.Oidc.ClientHandler"] = verbose ? "Trace" : "Warning", // add what we need
    [$"Logging:LogLevel:System.Net.Http.HttpClient.{nameof(FaluCliClient)}.ClientHandler"] = verbose ? "Trace" : "Warning", // add what we need
    ["Logging:LogLevel:System.Net.Http.HttpClient.Updates.ClientHandler"] = builder.Environment.IsDevelopment() && verbose ? "Trace" : "Warning", // add what we need

    ["Logging:Console:FormatterName"] = "Falu",
});

// configure logging
// not using AddConsoleFormatter<Falu.Logging.FaluConsoleFormatter, Falu.Logging.FaluConsoleFormatterOptions>() because it requires dynamic code
builder.Services.AddSingleton<Microsoft.Extensions.Logging.Console.ConsoleFormatter, Falu.Logging.FaluConsoleFormatter>();
builder.Services.Configure<Falu.Logging.FaluConsoleFormatterOptions>(options =>
{
    options.SingleLine = !verbose;
    options.IncludeCategory = verbose;
    options.IncludeEventId = verbose;
    options.TimestampFormat = "HH:mm:ss ";
});

// register services
builder.Services.AddSingleton(configValuesLoader);
builder.Services.AddSingleton(configValues);
builder.Services.AddSingleton(parseResult);
builder.Services.AddSingleton<IHostLifetime, InvocationLifetime>();
builder.Services.AddTransient<WebsocketHandler>();

// register API client
builder.Services.AddFalu<FaluCliClient, FaluClientOptions>()
                .AddHttpMessageHandler<FaluCliClientHandler>()
                .ConfigureHttpClientStandard(configValues);
builder.Services.AddTransient<FaluCliClientHandler>();
builder.Services.ConfigureAll<FaluClientOptions>(options =>
{
    // A dummy ApiKey is used so that the options validator can pass
    options.ApiKey = "dummy";
    options.Retries = configValues.Retries;
});

// register HttpClient for OIDC and Updates
builder.Services.AddHttpClient<OidcProvider>(name: "Oidc").ConfigureHttpClientStandard(configValues);
builder.Services.AddHttpClient(name: "Updates").ConfigureHttpClientStandard(configValues);

// register open telemetry unless disabled
var disabled = command?.IsNoTelemetry(parseResult) ?? configValues.NoTelemetry;
if (!disabled)
{
    builder.Services.AddOpenTelemetry()
                    .ConfigureResource(resource =>
                    {
                        resource.AddAttributes([new("environment", builder.Environment.EnvironmentName)]);
                        resource.AddDetector(new OpenTelemetry.ResourceDetectors.Host.HostDetector());
                        resource.AddDetector(new OpenTelemetry.ResourceDetectors.ProcessRuntime.ProcessRuntimeDetector());
                        resource.AddService(serviceName: Constants.ProductName, serviceVersion: VersioningHelper.ProductVersion);
                    })
                    .WithTracing(tracing =>
                    {
                        tracing.AddSource("System.CommandLine");
                        tracing.AddHttpClientInstrumentation();

                        // add exporter to Azure Monitor
                        var cs = builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"] ?? Constants.AppInsightsConnectionString;
                        tracing.AddAzureMonitorTraceExporter(options => options.ConnectionString = cs);
                    });
}

// build the host
using var host = builder.Build();

// set the action to be executed
parseResult.CommandResult.Command.Action = new FaluRootCliAction(configValuesLoader, configValues, host);

// run the command
return await parseResult.InvokeAsync();
