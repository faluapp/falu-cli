using Falu.Core;
using SC = Falu.FaluCliJsonSerializerContext;

namespace Falu.Client.Workspaces;

internal class WorkspacesServiceClient(HttpClient backChannel, FaluClientOptions options) : BaseServiceClient<Workspace>(backChannel, options, SC.Default.Workspace, SC.Default.ListWorkspace),
                                                                                            ISupportsListing<Workspace, WorkspacesListOptions>
{
    /// <inheritdoc/>
    protected override string BasePath => "/v1/workspaces";


    /// <summary>Retrieve workspaces.</summary>
    /// <inheritdoc/>
    public virtual Task<ResourceResponse<List<Workspace>>> ListAsync(WorkspacesListOptions? options = null,
                                                                     RequestOptions? requestOptions = null,
                                                                     CancellationToken cancellationToken = default)
    {
        return ListResourcesAsync(options, requestOptions, cancellationToken);
    }

    /// <summary>Retrieve workspaces recursively.</summary>
    /// <inheritdoc/>
    public virtual IAsyncEnumerable<Workspace> ListRecursivelyAsync(WorkspacesListOptions? options = null,
                                                                    RequestOptions? requestOptions = null,
                                                                    CancellationToken cancellationToken = default)
    {
        return ListResourcesRecursivelyAsync(options, requestOptions, cancellationToken);
    }
}
