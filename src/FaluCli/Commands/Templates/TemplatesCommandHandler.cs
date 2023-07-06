using Falu.Client;
using Falu.MessageTemplates;
using Spectre.Console;
using System.Text.Json;
using Tingle.Extensions.JsonPatch;

namespace Falu.Commands.Templates;

internal class TemplatesCommandHandler : ICommandHandler
{
    private const string BodyFileName = "content.txt";
    private const string InfoFileName = "info.json";

    private readonly FaluCliClient client;
    private readonly ILogger logger;

    public TemplatesCommandHandler(FaluCliClient client, ILogger<TemplatesCommandHandler> logger)
    {
        this.client = client ?? throw new ArgumentNullException(nameof(client));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    int ICommandHandler.Invoke(InvocationContext context) => throw new NotImplementedException();

    public Task<int> InvokeAsync(InvocationContext context)
    {
        var command = context.ParseResult.CommandResult.Command;
        if (command is TemplatesPullCommand) return HandlePullAsync(context);
        else if (command is TemplatesPushCommand) return HandlePushAsync(context);
        throw new InvalidOperationException($"Command of type '{command.GetType().FullName}' is not supported here.");
    }

    #region Pulling

    private async Task<int> HandlePullAsync(InvocationContext context)
    {
        var cancellationToken = context.GetCancellationToken();
        var outputPath = context.ParseResult.ValueForArgument<string>("output-directory")!;
        var overwrite = context.ParseResult.ValueForOption<bool>("--overwrite");

        // download the templates
        var templates = await DownloadTemplatesAsync(cancellationToken);

        // work on each template
        var saved = 0;
        foreach (var template in templates)
        {
            if (string.IsNullOrWhiteSpace(template.Alias))
            {
                logger.LogWarning("Template '{TemplateId}' without an alias shall be skipped.", template.Id);
                continue;
            }

            await SaveTemplateAsync(template, outputPath, overwrite, cancellationToken);
            saved++;
        }

        logger.LogInformation("Finished saving {Save} of {Total} templates to {OutputDirectory}", saved, templates.Count, outputPath);

        return 0;
    }

    private async Task SaveTemplateAsync(MessageTemplate template, string outputPath, bool overwrite, CancellationToken cancellationToken)
    {
        // create directory if it does not exist
        var dirPath = Path.Combine(outputPath, template.Alias!);
        if (!Directory.Exists(dirPath)) Directory.CreateDirectory(dirPath);

        // write the template body
        var contentPath = Path.Combine(dirPath, BodyFileName);
        await WriteToFileAsync(contentPath, overwrite, template.Body!, cancellationToken);

        // write the template info
        var infoPath = Path.Combine(dirPath, InfoFileName);
        var info = new TemplateInfo(template);
        using var stream = new MemoryStream();
        await JsonSerializer.SerializeAsync(stream, info, FaluCliJsonSerializerContext.Default.TemplateInfo, cancellationToken);
        stream.Seek(0, SeekOrigin.Begin);
        await WriteToFileAsync(infoPath, overwrite, stream, cancellationToken);
    }

    private Task WriteToFileAsync(string path, bool overwrite, string contents, CancellationToken cancellationToken)
    {
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(contents));
        return WriteToFileAsync(path, overwrite, stream, cancellationToken);
    }

    private async Task WriteToFileAsync(string path, bool overwrite, Stream contents, CancellationToken cancellationToken)
    {
        var exists = File.Exists(path);
        if (exists && !overwrite)
        {
            logger.LogWarning("Skipping overwrite for {Path}", path);
            return;
        }

        using var stream = File.OpenWrite(path);
        await contents.CopyToAsync(stream, cancellationToken);
    }

    #endregion

    #region Pushing

    public async Task<int> HandlePushAsync(InvocationContext context)
    {
        var cancellationToken = context.GetCancellationToken();
        var templatesDirectory = context.ParseResult.ValueForArgument<string>("templates-directory")!;
        var all = context.ParseResult.ValueForOption<bool>("--all");

        // ensure the directory exists
        if (!Directory.Exists(templatesDirectory))
        {
            logger.LogError("The directory {TemplatesDirectory} does not exist.", templatesDirectory);
            return -1;
        }

        // download the templates
        var templates = await DownloadTemplatesAsync(cancellationToken);

        // read manifests
        var manifests = await ReadManifestsAsync(templatesDirectory, cancellationToken);
        if (all)
        {
            logger.LogInformation("Pushing {Count} templates to Falu servers.", manifests.Count);
            await PushTemplatesAsync(manifests, cancellationToken);
        }
        else
        {
            GenerateChanges(templates, manifests);
            var modified = manifests.Where(m => m.ChangeType != ChangeType.Unmodified).ToList();
            logger.LogInformation("Pushing {Count} templates to Falu servers.", modified.Count);

            var table = new Table().AddColumn("Change")
                                   .AddColumn("Alias")
                                   .AddColumn("Id");

            foreach (var m in modified) table.AddRow(new Markup(ColorizeChangeType(m.ChangeType)), new Markup(m.Alias ?? "-"), new Markup(m.Id ?? "-"));
            AnsiConsole.Write(table);

            // TODO: seek prompt to push the changes (with an option override: -y/--yes)

            await PushTemplatesAsync(modified, cancellationToken);
        }

        return 0;
    }

    private static string ColorizeChangeType(ChangeType changeType)
    {
        return changeType switch
        {
            ChangeType.Added => SpectreFormatter.ColouredRed("Added"),
            ChangeType.Modified => SpectreFormatter.ColouredYellow("Modified"),
            _ => throw new InvalidOperationException($"Unknown change type '{nameof(ChangeType)}.{changeType}'")
        };
    }

    private async Task PushTemplatesAsync(IReadOnlyList<TemplateManifest> manifests, CancellationToken cancellationToken)
    {
        foreach (var mani in manifests)
        {
            if (mani.ChangeType == ChangeType.Added)
            {
                // prepare the request and send to server
                var request = new MessageTemplateCreateRequest
                {
                    Alias = mani.Alias,
                    Body = mani.Body,
                    Description = mani.Info.Description,
                    Metadata = mani.Info.Metadata,
                };
                await client.MessageTemplates.CreateAsync(request, cancellationToken: cancellationToken);
            }
            else if (mani.ChangeType == ChangeType.Modified)
            {
                // prepare the patch details and send to server
                var patch = new JsonPatchDocument<MessageTemplatePatchModel>()
                    .Replace(mt => mt.Alias, mani.Alias)
                    .Replace(mt => mt.Body, mani.Body)
                    .Replace(mt => mt.Description, mani.Info.Description)
                    .Replace(mt => mt.Metadata, mani.Info.Metadata);
                await client.MessageTemplates.UpdateAsync(mani.Id!, patch, cancellationToken: cancellationToken);
            }
        }
    }

    private static void GenerateChanges(in IReadOnlyList<MessageTemplate> templates, in IReadOnlyList<TemplateManifest> manifests)
    {
        if (templates is null) throw new ArgumentNullException(nameof(templates));
        if (manifests is null) throw new ArgumentNullException(nameof(manifests));

        foreach (var local in manifests)
        {
            // check if the manifest has a matching template in the workspace
            var remote = templates.SingleOrDefault(t => string.Equals(t.Alias, local.Alias, StringComparison.OrdinalIgnoreCase));
            if (remote is null)
            {
                local.ChangeType = ChangeType.Added;
                continue;
            }

            local.Id = remote.Id;
            local.ChangeType = HasChanged(remote, local) ? ChangeType.Modified : ChangeType.Unmodified;
        }
    }

    private static bool HasChanged(MessageTemplate remote, TemplateManifest local)
    {
        var bodyChanged = !string.Equals(remote.Body, local.Body, StringComparison.InvariantCulture);
        var descriptionChanged = !string.Equals(remote.Description, local.Info.Description, StringComparison.InvariantCulture);

        // if either is null or the counts are different, metadata changed
        var metadataChanged = (remote.Metadata is null && local.Info.Metadata is not null)
                              || (remote.Metadata is not null && local.Info.Metadata is null)
                              || (remote.Metadata?.Count != local.Info.Metadata?.Count);
        if (!metadataChanged && remote.Metadata is not null && local.Info.Metadata is not null)
        {
            // if a key does not exist or the value does not match, it changed
            foreach (var kvp in local.Info.Metadata)
            {
                if (!remote.Metadata.TryGetValue(kvp.Key, out var value)
                    || !string.Equals(kvp.Value, value, StringComparison.InvariantCulture))
                {
                    metadataChanged = true;
                    break;
                }
            }
        }

        return bodyChanged || descriptionChanged || metadataChanged;
    }

    private static async Task<IReadOnlyList<TemplateManifest>> ReadManifestsAsync(string templatesDirectory, CancellationToken cancellationToken)
    {
        var results = new List<TemplateManifest>();
        var directories = Directory.EnumerateDirectories(templatesDirectory);
        foreach (var dirPath in directories)
        {
            // there is no info file, we skip the folder/directory
            var infoPath = Path.Combine(dirPath, InfoFileName);
            if (!File.Exists(infoPath)) continue;

            // read the info
            using var stream = File.OpenRead(infoPath);
            var info = (await JsonSerializer.DeserializeAsync(stream, FaluCliJsonSerializerContext.Default.TemplateInfo, cancellationToken))!;

            var contentPath = Path.Combine(dirPath, BodyFileName);
            var body = await ReadFromFileAsync(contentPath, cancellationToken);

            results.Add(new TemplateManifest(info, body));
        }

        return results;
    }

    private static async Task<string> ReadFromFileAsync(string path, CancellationToken cancellationToken)
    {
        using var stream = File.OpenRead(path);
        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync(cancellationToken);
    }

    #endregion

    private async Task<IReadOnlyList<MessageTemplate>> DownloadTemplatesAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Fetching templates ...");
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
