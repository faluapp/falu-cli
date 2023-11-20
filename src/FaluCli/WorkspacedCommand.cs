using Res = Falu.Properties.Resources;

namespace Falu;

public class WorkspacedCommand : Command
{
    public WorkspacedCommand(string name, string? description = null) : base(name, description)
    {
        this.AddGlobalOption(aliases: ["--apikey"],
                             description: Res.OptionDescriptionApiKey,
                             format: Constants.ApiKeyFormat);

        this.AddGlobalOption(aliases: ["--workspace"],
                             description: Res.OptionDescriptionWorkspace,
                             format: Constants.WorkspaceIdFormat);

        // without this the nullable type, the option is not found because we have not migrated to the new bindings
        this.AddGlobalOption<bool?>(aliases: ["--live"],
                                    description: Res.OptionDescriptionLive);

        this.AddGlobalOption(aliases: ["--idempotency-key"],
                             description: Res.OptionDescriptionIdempotencyKey,
                             format: Constants.IdempotencyKeyFormat);
    }
}
