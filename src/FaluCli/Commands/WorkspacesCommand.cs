using Falu.Config;
using Spectre.Console;

namespace Falu.Commands;

internal class WorkspacesCommand : FaluCliCommand
{
    public WorkspacesCommand() : base("workspaces", "Manage workspaces")
    {
        Add(new WorkspacesListCommand());
        Add(new WorkspacesShowCommand());
    }
}

internal class WorkspacesListCommand : FaluExecuteableCliCommand
{
    private readonly CliOption<bool> allOption;
    private readonly CliOption<bool> refreshOption;

    public WorkspacesListCommand() : base("list", "Get a list of workspaces for the logged in account.\r\nBy default, 'Terminated' workspaces are not shown.")
    {
        allOption = new CliOption<bool>(name: "--all")
        {
            Description = "List all workspaces, rather than skipping 'Terminated' ones.",
            DefaultValueFactory = r => false,
        };
        Add(allOption);

        refreshOption = new CliOption<bool>(name: "--refresh")
        {
            Description = "Retrieve up-to-date workspaces from server.",
            DefaultValueFactory = r => false,
        };
        Add(refreshOption);
    }

    public override async Task<int> ExecuteAsync(CliCommandExecutionContext context, CancellationToken cancellationToken)
    {
        var all = context.ParseResult.GetValue(allOption);
        var refresh = context.ParseResult.GetValue(refreshOption);

        var defaultWorkspaceId = context.ConfigValues.DefaultWorkspaceId;

        var workspaces = context.ConfigValues.Workspaces.ToList();
        if (refresh)
        {
            var response = await context.Client.Workspaces.ListAsync(cancellationToken: cancellationToken);
            response.EnsureSuccess();
            workspaces = response.Resource!.Select(w => new ConfigValuesWorkspace(w)).ToList();
            context.ConfigValues.Workspaces = workspaces; // update them so that they are saved

            // update default workspace
            if (workspaces.Count > 0)
            {
                if (defaultWorkspaceId is not null)
                {
                    var workspace = context.ConfigValues.GetWorkspace(defaultWorkspaceId);
                    if (workspace is null)
                    {
                        context.Logger.LogInformation("Default workspace '{DefaultWorkspaceId}' not found. Resetting to null.", defaultWorkspaceId);
                        defaultWorkspaceId = context.ConfigValues.DefaultWorkspaceId = null;
                    }
                }
            }
        }

        workspaces = all ? workspaces : workspaces.Where(w => !string.Equals(w.Status, "terminated", StringComparison.OrdinalIgnoreCase)).ToList();

        var table = new Table().AddColumn("Name")
                               .AddColumn("Id")
                               .AddColumn("Status")
                               .AddColumn(new TableColumn("Default").Centered());

        foreach (var workspace in workspaces)
        {
            var isDefault = string.Equals(workspace.Id, defaultWorkspaceId, StringComparison.OrdinalIgnoreCase);
            table.AddRow(new Markup(workspace.Name),
                         new Markup(workspace.Id),
                         new Markup(workspace.Status),
                         new Markup(isDefault ? SpectreFormatter.ColouredGreen("✔ YES") : ""));
        }

        AnsiConsole.Write(table);

        return 0;
    }
}

internal class WorkspacesShowCommand : FaluExecuteableCliCommand
{
    private readonly CliOption<string> nameOption;

    public WorkspacesShowCommand() : base("show", " Get the details of a workspace.")
    {
        nameOption = new CliOption<string>(name: "--name", ["-n"]) { Description = "Name or ID of workspace.", Required = true, };
        Add(nameOption);
    }

    public override Task<int> ExecuteAsync(CliCommandExecutionContext context, CancellationToken cancellationToken)
    {
        var name = context.ParseResult.GetValue(nameOption)!;

        var workspace = context.ConfigValues.GetRequiredWorkspace(name);

        var table = new Table().AddColumn("Name")
                               .AddColumn("Id")
                               .AddColumn("Status")
                               .AddColumn(new TableColumn("Default").Centered());

        var isDefault = string.Equals(workspace.Id, context.ConfigValues.DefaultWorkspaceId, StringComparison.OrdinalIgnoreCase);
        table.AddRow(new Markup(workspace.Name),
                     new Markup(workspace.Id),
                     new Markup(workspace.Status),
                     new Markup(isDefault ? SpectreFormatter.ColouredGreen("✔ YES") : ""));

        AnsiConsole.Write(table);

        return Task.FromResult(0);
    }
}
