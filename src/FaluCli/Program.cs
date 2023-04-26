using Falu;
using Falu.Commands.Config;
using Falu.Commands.Events;
using Falu.Commands.Login;
using Falu.Commands.Messages;
using Falu.Commands.Money;
using Falu.Commands.Templates;
using System.CommandLine.Builder;
using System.CommandLine.Hosting;

// Create a root command with some options
var rootCommand = new RootCommand
{
    new LoginCommand(),
    new LogoutCommand(),

    new WorkspacedCommand("events", "Work with events on Falu.")
    {
        new RetryCommand(),
    },

    new WorkspacedCommand("messages", "Work with messages.")
    {
        new Command("send", "Send messages.")
        {
            new SendRawMessagesCommand(),
            new SendTemplatedMessagesCommand(),
        }
    },

    new WorkspacedCommand("templates", "Manage message templates.")
    {
        new PullTemplatesCommand(),
        new PushTemplatesCommand(),
    },

    new WorkspacedCommand("payments", "Manage payments.")
    {
        new UploadMpesaStatementCommand(FaluObjectKind.Payments),
    },

    new WorkspacedCommand("payment-refunds", "Manage payment refunds.")
    {
        new UploadMpesaStatementCommand(FaluObjectKind.PaymentRefunds),
    },

    new WorkspacedCommand("transfers", "Manage transfers.")
    {
        new UploadMpesaStatementCommand(FaluObjectKind.Transfers),
    },

    new WorkspacedCommand("transfer-reversals", "Manage transfer reversals.")
    {
        new UploadMpesaStatementCommand(FaluObjectKind.TransferReversals),
    },

    new Command("config", "Manage configuration for the CLI.")
    {
        new ConfigShowCommand(),
        new ConfigSetCommand(),
        new Command("clear", "Clear configuration for the CLI.")
        {
            new ConfigClearAuthCommand(),
            new ConfigClearAllCommand(),
        },
    },
};

rootCommand.Description = "Official CLI tool for Falu.";
rootCommand.AddGlobalOption(new[] { "-v", "--verbose" }, "Whether to output verbosely.", false);

var builder = new CommandLineBuilder(rootCommand)
    .UseHost(_ => Host.CreateDefaultBuilder(args), host =>
    {
        host.ConfigureAppConfiguration((context, builder) =>
        {
            var iv = context.GetInvocationContext();
            var verbose = iv.IsVerboseEnabled();

            builder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Logging:LogLevel:Default"] = "Information",
                ["Logging:LogLevel:Microsoft"] = "Warning",

                // See https://docs.microsoft.com/en-us/aspnet/core/fundamentals/http-requests?view=aspnetcore-6.0#logging
                ["Logging:LogLevel:System.Net.Http.HttpClient"] = "None", // removes all we do not need
                ["Logging:LogLevel:System.Net.Http.HttpClient.Oidc.ClientHandler"] = verbose ? "Trace" : "Warning", // add what we need
                ["Logging:LogLevel:System.Net.Http.HttpClient.FaluCliClient.ClientHandler"] = verbose ? "Trace" : "Warning", // add what we need

                ["Logging:Console:FormatterName"] = "Falu",
                ["Logging:Console:FormatterOptions:SingleLine"] = verbose ? "False" : "True",
                ["Logging:Console:FormatterOptions:IncludeCategory"] = verbose ? "True" : "False",
                ["Logging:Console:FormatterOptions:IncludeEventId"] = verbose ? "True" : "False",
                ["Logging:Console:FormatterOptions:TimestampFormat"] = "HH:mm:ss ",
            });
        });

        host.ConfigureLogging((context, builder) =>
        {
            builder.AddConsoleFormatter<Falu.Logging.FaluConsoleFormatter, Falu.Logging.FaluConsoleFormatterOptions>();
        });

        host.ConfigureServices((context, services) =>
        {
            var configuration = context.Configuration;
            services.AddFaluClientForCli();
            services.AddUpdateChecker();
            services.AddConfigValuesProvider();
            services.AddOpenIdServices();
        });

        host.UseCommandHandler<LoginCommand, LoginCommandHandler>();
        host.UseCommandHandler<LogoutCommand, LogoutCommandHandler>();
        host.UseCommandHandler<RetryCommand, RetryCommandHandler>();
        host.UseCommandHandler<SendRawMessagesCommand, SendMessagesCommandHandler>();
        host.UseCommandHandler<SendTemplatedMessagesCommand, SendMessagesCommandHandler>();
        host.UseCommandHandler<PullTemplatesCommand, TemplatesCommandHandler>();
        host.UseCommandHandler<PushTemplatesCommand, TemplatesCommandHandler>();
        host.UseCommandHandler<UploadMpesaStatementCommand, UploadMpesaStatementCommandHandler>();
        host.UseCommandHandler<ConfigShowCommand, ConfigCommandHandler>();
        host.UseCommandHandler<ConfigSetCommand, ConfigCommandHandler>();
        host.UseCommandHandler<ConfigClearAllCommand, ConfigCommandHandler>();
        host.UseCommandHandler<ConfigClearAuthCommand, ConfigCommandHandler>();
    })
    .UseFaluDefaults()
    .UseUpdateChecker() /* update checker middleware must be added last because it only prints what the checker has */;

// Parse the incoming args and invoke the handler
var parser = builder.Build();
return await parser.InvokeAsync(args);
