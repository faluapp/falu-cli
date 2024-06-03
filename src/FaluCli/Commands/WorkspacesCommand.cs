using Falu.Config;
using Spectre.Console;

namespace Falu.Commands;

internal class WorkspacesCommand : CliCommand
{
    public WorkspacesCommand() : base("workspaces", "Manage workspaces")
    {
        Add(new WorkspacesListCommand());
        Add(new WorkspacesShowCommand());
    }
}

internal class WorkspacesListCommand : FaluCliCommand
{
    public WorkspacesListCommand() : base("list", "Get a list of workspaces for the logged in account.\r\nBy default, 'Terminated' workspaces are not shown.")
    {
        this.AddOption(aliases: ["--all"],
                       description: "List all workspaces, rather than skipping 'Terminated' ones.",
                       defaultValue: false);

        this.AddOption(aliases: ["--refresh"],
                       description: "Retrieve up-to-date workspaces from server.",
                       defaultValue: false);
    }

    public override async Task<int> ExecuteAsync(CliCommandExecutionContext context, CancellationToken cancellationToken)
    {
        var all = context.ParseResult.ValueForOption<bool>("--all");
        var refresh = context.ParseResult.ValueForOption<bool>("--refresh");

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

internal class WorkspacesShowCommand : FaluCliCommand
{
    public WorkspacesShowCommand() : base("show", " Get the details of a workspace.")
    {
        this.AddOption<string>(aliases: ["--name", "-n"],
                               description: "Name or ID of workspace.",
                               configure: o => o.Required = true);
    }

    public override Task<int> ExecuteAsync(CliCommandExecutionContext context, CancellationToken cancellationToken)
    {
        var name = context.ParseResult.ValueForOption<string>("--name")!;

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
