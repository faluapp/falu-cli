using Falu.Core;

namespace Falu.Client.Workspaces;

internal class Workspace : IHasId
{
    /// <inheritdoc/>
    public string? Id { get; set; }

    public string? Name { get; set; }

    public string? Status { get; set; }

    public string? Role { get; set; }
}
