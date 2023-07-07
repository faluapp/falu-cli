using Res = Falu.Properties.Resources;

namespace Falu;

public class WorkspacedCommand : Command
{
    public WorkspacedCommand(string name, string? description = null) : base(name, description)
    {
        this.AddGlobalOption(aliases: new[] { "--apikey", },
                             description: Res.OptionDescriptionApiKey,
                             format: Constants.ApiKeyFormat);

        this.AddGlobalOption(aliases: new[] { "--workspace", },
                             description: Res.OptionDescriptionWorkspace,
                             format: Constants.WorkspaceIdFormat);

        // without this the nullable type, the option is not found because we have not migrated to the new bindings
        this.AddGlobalOption<bool?>(aliases: new[] { "--live", },
                                    description: Res.OptionDescriptionLive);

        this.AddGlobalOption(aliases: new[] { "--idempotency-key", },
                             description: Res.OptionDescriptionIdempotencyKey,
                             format: Constants.IdempotencyKeyFormat);
    }
}
