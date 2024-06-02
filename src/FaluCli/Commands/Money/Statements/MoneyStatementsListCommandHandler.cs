using Falu.Client;
using Falu.Client.MoneyStatements;
using Spectre.Console;

namespace Falu.Commands.Money.Statements;

internal class MoneyStatementsListCommandHandler(FaluCliClient client) : ICommandHandler
{
    int ICommandHandler.Invoke(InvocationContext context) => throw new NotImplementedException();

    public async Task<int> InvokeAsync(InvocationContext context)
    {
        var cancellationToken = context.GetCancellationToken();
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
            table.AddRow(new Markup(statement.Id!),
                         new Markup($"{statement.Created.ToLocalTime():F}"),
                         new Markup(statement.Provider!).Centered(),
                         new Markup(kind!).Centered(),
                         new Markup(statement.Uploaded.ToString().ToLower()).Centered());
        }

        AnsiConsole.Write(table);

        return 0;
    }
}
