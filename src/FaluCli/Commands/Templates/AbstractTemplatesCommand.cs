using Falu.Client;
using Falu.MessageTemplates;

namespace Falu.Commands.Templates;

internal class AbstractTemplatesCommand(string name, string? description = null) : Command(name, description)
{
    internal static async Task<IReadOnlyList<MessageTemplate>> DownloadTemplatesAsync(FaluCliClient client, ILogger logger, CancellationToken cancellationToken)
    {
        logger.LogInformation("Fetching templates from server ...");
        var result = new List<MessageTemplate>();
        var options = new MessageTemplatesListOptions { Count = 100, };
        var templates = client.MessageTemplates.ListRecursivelyAsync(options, cancellationToken: cancellationToken);
        await foreach (var template in templates)
        {
            result.Add(template);
        }
        logger.LogInformation("Received {Count} templates.", result.Count);

        return result;
    }
}
