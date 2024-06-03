using Falu.Client;
using Falu.Client.MoneyStatements;
using Spectre.Console;

namespace Falu.Commands.Money.Statements;

public class MoneyStatementsListCommand : Command
{
    public MoneyStatementsListCommand() : base("list", "List recent money statements")
    {
        this.AddOption<string[]>(["--object-kind"],
                                 description: "The object type to filter statements for.",
                                 configure: o => o.FromAmong("payments", "payment_refunds", "transfers", "transfer_reversals"));

        this.AddOption<string[]>(["--provider"],
                                 description: "Type of provider to filter statements for.",
                                 configure: o => o.FromAmong("mpesa"));

        this.AddOption<bool?>(["--uploaded"],
                              description: "Whether to only list uploaded statements");

        this.AddOption(["--count"],
                       description: "Number of records to retrieve",
                       defaultValue: 10);

        this.SetHandler(HandleAsync);
    }

    private async Task HandleAsync(InvocationContext context)
    {
        var cancellationToken = context.GetCancellationToken();
        var client = context.GetRequiredService<FaluCliClient>();

        var objectKinds = context.ParseResult.ValueForOption<string[]?>("--object-kind")!;
        var providers = context.ParseResult.ValueForOption<string[]?>("--provider")!;
        var uploaded = context.ParseResult.ValueForOption<bool?>("--uploaded")!;
        var count = context.ParseResult.ValueForOption<int>("--count")!;

        var options = new MoneyStatementsListOptions
        {
            Provider = providers?.ToList(),
            ObjectsKind = objectKinds?.ToList(),
            Uploaded = uploaded,
            Sorting = "desc",
            Count = count,
        };
        var response = await client.MoneyStatements.ListAsync(options);
        response.EnsureSuccess();

        var statements = response.Resource!;

        // Create a table
        var table = new Table().AddColumn("Id")
                               .AddColumn("Created")
                               .AddColumn(new TableColumn("Provider").Centered())
                               .AddColumn(new TableColumn("Objects Kind").Centered())
                               .AddColumn(new TableColumn("Uploaded").Centered());

        // Add rows
        foreach (var statement in statements)
        {
            var kind = statement.ObjectsKind switch
            {
                "payments" => "Payments",
                "payment_refunds" => "Payment Refunds",
                "transfers" => "Transfers",
                "transfer_reversals" => "Transfer Reversals",
                _ => statement.ObjectsKind,
            };
            table.AddRow(new Markup(statement.Id!), new Markup($"{statement.Created.ToLocalTime():F}"), new Markup(statement.Provider!).Centered(), new Markup(kind!).Centered(), new Markup(statement.Uploaded.ToString().ToLower()).Centered());
        }

        AnsiConsole.Write(table);
    }
}
