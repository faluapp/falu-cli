using Falu.Client.MoneyStatements;
using Spectre.Console;

namespace Falu.Commands.Money.Statements;

internal class MoneyStatementsListCommand : WorkspacedCommand
{
    private readonly CliOption<string[]> objectKindOption;
    private readonly CliOption<string[]> providerOption;
    private readonly CliOption<bool?> uploadedOption;
    private readonly CliOption<int> countOption;

    public MoneyStatementsListCommand() : base("list", "List recent money statements")
    {
        objectKindOption = new CliOption<string[]>(name: "--object-kind") { Description = "The object type to filter statements for.", };
        objectKindOption.AcceptOnlyFromAmong("payments", "payment_refunds", "transfers", "transfer_reversals");
        Add(objectKindOption);

        providerOption = new CliOption<string[]>(name: "--provider") { Description = "Type of provider to filter statements for.", };
        providerOption.AcceptOnlyFromAmong("mpesa");
        Add(providerOption);

        uploadedOption = new CliOption<bool?>(name: "--uploaded") { Description = "Whether to only list uploaded statements", };
        Add(uploadedOption);

        countOption = new CliOption<int>(name: "--count") { Description = "Number of records to retrieve", DefaultValueFactory = r => 10, };
        Add(countOption);
    }

    public override async Task<int> ExecuteAsync(CliCommandExecutionContext context, CancellationToken cancellationToken)
    {
        var objectKinds = context.ParseResult.GetValue(objectKindOption);
        var providers = context.ParseResult.GetValue(providerOption);
        var uploaded = context.ParseResult.GetValue(uploadedOption);
        var count = context.ParseResult.GetValue(countOption);

        var options = new MoneyStatementsListOptions
        {
            Provider = providers?.ToList(),
            ObjectsKind = objectKinds?.ToList(),
            Uploaded = uploaded,
            Sorting = "desc",
            Count = count,
        };
        var response = await context.Client.MoneyStatements.ListAsync(options, cancellationToken: cancellationToken);
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
                         new Markup(statement.Uploaded.ToString().ToLowerInvariant()).Centered());
        }

        AnsiConsole.Write(table);

        return 0;
    }
}
