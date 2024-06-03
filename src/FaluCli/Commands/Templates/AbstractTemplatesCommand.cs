using Falu.MessageTemplates;

namespace Falu.Commands.Templates;

internal abstract class AbstractTemplatesCommand(string name, string? description = null) : WorkspacedCommand(name, description)
{
    internal static async Task<IReadOnlyList<MessageTemplate>> DownloadTemplatesAsync(CliCommandExecutionContext context, CancellationToken cancellationToken)
    {
        context.Logger.LogInformation("Fetching templates from server ...");
        var result = new List<MessageTemplate>();
        var options = new MessageTemplatesListOptions { Count = 100, };
        var templates = context.Client.MessageTemplates.ListRecursivelyAsync(options, cancellationToken: cancellationToken);
        await foreach (var template in templates)
        {
            result.Add(template);
        }
        context.Logger.LogInformation("Received {Count} templates.", result.Count);

        return result;
    }
}
