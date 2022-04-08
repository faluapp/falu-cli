﻿using Falu.Commands.Events;
using Falu.Commands.Login;
using Falu.Commands.Money;
using Falu.Commands.Templates;
using System.CommandLine.Builder;
using System.CommandLine.Hosting;
using System.CommandLine.Parsing;

// Create a root command with some options
var rootCommand = new RootCommand
{
    new LoginCommand(),

    new Command("evaluations", "Manage evaluations.")
    {
    },

    new Command("messages", "Manage messages.")
    {
    },

    new Command("templates", "Manage message templates.")
    {
        new PullTemplatesCommand(),
        new PushTemplatesCommand(),
    },

    new Command("payments", "Manage payments.")
    {
        new UploadMpesaPaymentsStatementCommand(),
    },

    new Command("payment-refunds", "Manage payment refunds.")
    {
        new UploadMpesaPaymentRefundsStatementCommand(),
    },

    new Command("transfers", "Manage transfers.")
    {
        new UploadMpesaTransfersStatementCommand(),
    },

    new Command("transfer-reversals", "Manage transfer reversals.")
    {
        new UploadMpesaTransferReversalsStatementCommand(),
    },

    new Command("events", "Work with events on Falu.")
    {
        new RetryCommand(),
    },

    new Command("webhooks", "Manage webhooks.")
    {
    },
};

rootCommand.Description = "Official CLI tool for Falu.";
rootCommand.AddCommonGlobalOptions();

var builder = new CommandLineBuilder(rootCommand)
    .UseHost(_ => Host.CreateDefaultBuilder(args), host =>
    {
        host.ConfigureAppConfiguration((context, builder) =>
        {
            var iv = context.GetInvocationContext();
            var verbose = iv.IsVerboseEnabled();

            builder.AddInMemoryCollection(new Dictionary<string, string>
            {
                ["Logging:LogLevel:Default"] = "Information",
                ["Logging:LogLevel:Microsoft"] = "Warning",

                // See https://docs.microsoft.com/en-us/aspnet/core/fundamentals/http-requests?view=aspnetcore-5.0#logging
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
        host.UseCommandHandler<RetryCommand, RetryCommandHandler>();
        host.UseCommandHandler<PullTemplatesCommand, TemplatesCommandHandler>();
        host.UseCommandHandler<PushTemplatesCommand, TemplatesCommandHandler>();
        host.UseCommandHandler<UploadMpesaPaymentsStatementCommand, UploadMpesaStatementCommandHandler>();
        host.UseCommandHandler<UploadMpesaTransfersStatementCommand, UploadMpesaStatementCommandHandler>();
    })
    .UseFaluDefaults()
    .UseUpdateChecker() /* update checker middleware must be added last because it only prints what the checker has */;

// Parse the incoming args and invoke the handler
var parser = builder.Build();
return await parser.InvokeAsync(args);
