using Falu;
using Falu.Commands.Config;
using Falu.Commands.Events;
using Falu.Commands.Login;
using Falu.Commands.Messages;
using Falu.Commands.Money.Balances;
using Falu.Commands.Money.Statements;
using Falu.Commands.RequestLogs;
using Falu.Commands.Templates;
using Falu.Websockets;
using System.CommandLine.Builder;
using System.CommandLine.Hosting;
using Res = Falu.Properties.Resources;

// Create a root command with some options
var rootCommand = new RootCommand
{
    new LoginCommand(),
    new LogoutCommand(),

    new WorkspacedCommand("events", "Work with events on Falu.")
    {
        new EventsListenCommand(),
        new EventRetryCommand(),
    },

    new WorkspacedCommand("messages", "Work with messages.")
    {
        new Command("send", "Send messages.")
        {
            new MessagesSendRawCommand(),
            new MessagesSendTemplatedCommand(),
        }
    },

    new WorkspacedCommand("templates", "Manage message templates.")
    {
        new TemplatesPullCommand(),
        new TemplatesPushCommand(),
    },

    new WorkspacedCommand("money", "Work with money.")
    {
        new Command("balances", "Work with money balances.")
        {
            new MoneyBalancesGetCommand(),
            new MoneyBalancesRefreshCommand(),
        },
        new Command("statements", "Work with money statements.")
        {
            new MoneyStatementsListCommand(),
            //new MoneyStatementsGetCommand(),
            new MoneyStatementsUploadCommand(),
        },
    },

    new WorkspacedCommand("logs", "Work with request logs.")
    {
        new RequestLogsTailCommand(),
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
rootCommand.AddGlobalOption<bool?>(new[] { "--skip-update-checks", }, Res.OptionDescriptionSkipUpdateCheck); // nullable so as to allow checking if specified

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
                ["Logging:LogLevel:Microsoft.Hosting.Lifetime"] = "Warning",
                [$"Logging:LogLevel:{typeof(WebsocketHandler).FullName}"] = verbose ? "Trace" : "Information",

                // See https://docs.microsoft.com/en-us/aspnet/core/fundamentals/http-requests?view=aspnetcore-7.0#logging
                ["Logging:LogLevel:System.Net.Http.HttpClient"] = "None", // removes all we do not need
                ["Logging:LogLevel:System.Net.Http.HttpClient.Oidc.ClientHandler"] = verbose ? "Trace" : "Warning", // add what we need
                [$"Logging:LogLevel:System.Net.Http.HttpClient.{nameof(Falu.Client.FaluCliClient)}.ClientHandler"] = verbose ? "Trace" : "Warning", // add what we need
                [$"Logging:LogLevel:System.Net.Http.HttpClient.{nameof(Falu.Updates.UpdateChecker)}.ClientHandler"] = context.HostingEnvironment.IsDevelopment() && verbose ? "Trace" : "Warning", // add what we need

                ["Logging:Console:FormatterName"] = "Falu",
                ["Logging:Console:FormatterOptions:SingleLine"] = verbose ? "False" : "True",
                ["Logging:Console:FormatterOptions:IncludeCategory"] = verbose ? "True" : "False",
                ["Logging:Console:FormatterOptions:IncludeEventId"] = verbose ? "True" : "False",
                ["Logging:Console:FormatterOptions:TimestampFormat"] = "HH:mm:ss ",
            });
        });

        host.ConfigureLogging((context, builder) =>
        {
#pragma warning disable IL2026 // Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code
            builder.AddConsoleFormatter<Falu.Logging.FaluConsoleFormatter, Falu.Logging.FaluConsoleFormatterOptions>();
#pragma warning restore IL2026 // Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code
        });

        host.ConfigureServices((context, services) =>
        {
            var configuration = context.Configuration;
            services.AddFaluClientForCli();
            services.AddUpdateChecker();
            services.AddConfigValuesProvider();
            services.AddOpenIdProvider();
            services.AddTransient<WebsocketHandler>();
        });

        // System.CommandLine library does not create a scope, so we should skip validation of scopes
        host.UseDefaultServiceProvider(o => o.ValidateScopes = false);

        host.UseCommandHandlerTrimmable<LoginCommand, LoginCommandHandler>();
        host.UseCommandHandlerTrimmable<LogoutCommand, LogoutCommandHandler>();
        host.UseCommandHandlerTrimmable<EventsListenCommand, EventsListenCommandHandler>();
        host.UseCommandHandlerTrimmable<EventRetryCommand, EventRetryCommandHandler>();
        host.UseCommandHandlerTrimmable<MessagesSendRawCommand, MessagesSendCommandHandler>();
        host.UseCommandHandlerTrimmable<MessagesSendTemplatedCommand, MessagesSendCommandHandler>();
        host.UseCommandHandlerTrimmable<TemplatesPullCommand, TemplatesCommandHandler>();
        host.UseCommandHandlerTrimmable<TemplatesPushCommand, TemplatesCommandHandler>();
        host.UseCommandHandlerTrimmable<MoneyBalancesGetCommand, MoneyBalancesGetCommandHandler>();
        host.UseCommandHandlerTrimmable<MoneyBalancesRefreshCommand, MoneyBalancesRefreshCommandHandler>();
        host.UseCommandHandlerTrimmable<MoneyStatementsListCommand, MoneyStatementsListCommandHandler>();
        host.UseCommandHandlerTrimmable<MoneyStatementsUploadCommand, MoneyStatementsUploadCommandHandler>();
        host.UseCommandHandlerTrimmable<ConfigShowCommand, ConfigCommandHandler>();
        host.UseCommandHandlerTrimmable<ConfigSetCommand, ConfigCommandHandler>();
        host.UseCommandHandlerTrimmable<ConfigClearAllCommand, ConfigCommandHandler>();
        host.UseCommandHandlerTrimmable<ConfigClearAuthCommand, ConfigCommandHandler>();
        host.UseCommandHandlerTrimmable<RequestLogsTailCommand, RequestLogsTailCommandHandler>();
    })
    .UseFaluDefaults()
    .UseUpdateChecker() /* update checker middleware must be added last because it only prints what the checker has */;

// Parse the incoming args and invoke the handler
var parser = builder.Build();
return await parser.InvokeAsync(args);
