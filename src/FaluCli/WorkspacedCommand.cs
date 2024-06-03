using Res = Falu.Properties.Resources;

namespace Falu;

internal abstract class WorkspacedCommand : FaluCliCommand
{
    public WorkspacedCommand(string name, string? description = null) : base(name, description)
    {
        this.AddOption(aliases: ["--apikey"],
                       description: Res.OptionDescriptionApiKey,
                       format: Constants.ApiKeyFormat);

        this.AddOption(aliases: ["--workspace"],
                       description: Res.OptionDescriptionWorkspace,
                       format: Constants.WorkspaceIdFormat);

        // without this the nullable type, the option is not found because we have not migrated to the new bindings
        this.AddOption<bool?>(aliases: ["--live"],
                              description: Res.OptionDescriptionLive);

        this.AddOption(aliases: ["--idempotency-key"],
                       description: Res.OptionDescriptionIdempotencyKey,
                       format: Constants.IdempotencyKeyFormat);
    }
}
