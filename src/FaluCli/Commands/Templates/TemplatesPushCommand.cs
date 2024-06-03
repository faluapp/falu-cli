using Falu.Client;
using Falu.MessageTemplates;
using Spectre.Console;
using System.Text.Json;
using Tingle.Extensions.JsonPatch;

namespace Falu.Commands.Templates;

public class TemplatesPushCommand : AbstractTemplatesCommand
{
    public TemplatesPushCommand() : base("push", "Pushes changed templates from the local file system to Falu servers.")
    {
        this.AddArgument<string>(name: "templates-directory",
                                 description: "The directory containing the templates.");

        this.AddOption(["-a", "--all"],
                       description: "Push all local templates up to Falu regardless of whether they changed.",
                       defaultValue: false);

        this.SetHandler(HandleAsync);
    }

    private static async Task HandleAsync(InvocationContext context)
    {
        var cancellationToken = context.GetCancellationToken();
        var client = context.GetRequiredService<FaluCliClient>();
        var logger = context.GetRequiredService<ILogger<TemplatesPushCommand>>();

        var templatesDirectory = context.ParseResult.ValueForArgument<string>("templates-directory")!;
        var all = context.ParseResult.ValueForOption<bool>("--all");

        // ensure the directory exists
        if (!Directory.Exists(templatesDirectory))
        {
            logger.LogError("The directory {TemplatesDirectory} does not exist.", templatesDirectory);
            context.ExitCode = -1;
            return;
        }

        // download the templates
        var templates = await DownloadTemplatesAsync(client, logger, cancellationToken);

        // read manifests
        var manifests = await ReadManifestsAsync(logger, templatesDirectory, cancellationToken);
        if (all)
        {
            // TODO: seek prompt to push the changes (with an option override: -y/--yes)

            logger.LogInformation("Pushing {Count} templates to Falu servers.", manifests.Count);
            await PushTemplatesAsync(manifests, client, logger, cancellationToken);
        }
        else
        {
            GenerateChanges(logger, templates, manifests);
            var modified = manifests.Where(m => m.ChangeType != TemplateChangeType.Unmodified).ToList();
            if (modified.Count == 0)
            {
                logger.LogInformation("There are no changes to the templates.");
                return;
            }

            logger.LogInformation("Pushing {Count} templates to Falu servers.", modified.Count);

            var table = new Table().AddColumn("Change")
                                   .AddColumn("Alias")
                                   .AddColumn("Id");

            foreach (var m in modified) table.AddRow(new Markup(ColorizeChangeType(m.ChangeType)), new Markup(m.Alias ?? "-"), new Markup(m.Id ?? "-"));
            AnsiConsole.Write(table);

            // TODO: seek prompt to push the changes (with an option override: -y/--yes)

            await PushTemplatesAsync(modified, client, logger, cancellationToken);
        }
    }

    private static string ColorizeChangeType(TemplateChangeType changeType)
    {
        return changeType switch
        {
            TemplateChangeType.Added => SpectreFormatter.ColouredRed("Added"),
            TemplateChangeType.Modified => SpectreFormatter.ColouredYellow("Modified"),
            _ => throw new InvalidOperationException($"Unknown change type '{nameof(TemplateChangeType)}.{changeType}'")
        };
    }

    private static async Task PushTemplatesAsync(IReadOnlyList<TemplateManifest> manifests, FaluCliClient client, ILogger logger, CancellationToken cancellationToken)
    {
        foreach (var mani in manifests)
        {
            var changeType = mani.ChangeType;
            var alias = mani.Alias;
            if (changeType is TemplateChangeType.Unmodified)
            {
                logger.LogDebug("Template with alias {Alias} has not changes. Skipping it ...", alias);
                continue;
            }

            var body = mani.Body;
            var translations = mani.Translations.ToDictionary(p => p.Key, p => new MessageTemplateTranslation { Body = p.Value, });
            var description = mani.Info.Description;
            var metadata = mani.Info.Metadata;
            if (changeType is TemplateChangeType.Added)
            {
                // prepare the request and send to server
                var request = new MessageTemplateCreateRequest
                {
                    Alias = alias,
                    Body = body,
                    Translations = mani.Translations.ToDictionary(p => p.Key, p => new MessageTemplateTranslation { Body = p.Value, }),
                    Description = description,
                    Metadata = metadata,
                };
                logger.LogDebug("Creating template with alias {Alias} ...", alias);
                var response = await client.MessageTemplates.CreateAsync(request, cancellationToken: cancellationToken);
                response.EnsureSuccess();
                logger.LogDebug("Template with alias {Alias} created with Id: '{Id}'", alias, response.Resource!.Id);
            }
            else if (changeType is TemplateChangeType.Modified)
            {
                // prepare the patch details and send to server
                var patch = new JsonPatchDocument<MessageTemplatePatchModel>()
                    .Replace(mt => mt.Alias, alias)
                    .Replace(mt => mt.Body, body)
                    .Replace(mt => mt.Translations, translations)
                    .Replace(mt => mt.Description, description)
                    .Replace(mt => mt.Metadata, metadata);
                logger.LogDebug("Updating template with alias {Alias} ...", alias);
                var response = await client.MessageTemplates.UpdateAsync(mani.Id!, patch, cancellationToken: cancellationToken);
                response.EnsureSuccess();
            }
        }
    }

    private static void GenerateChanges(ILogger logger, in IReadOnlyList<MessageTemplate> templates, in IReadOnlyList<TemplateManifest> manifests)
    {
        ArgumentNullException.ThrowIfNull(templates);
        ArgumentNullException.ThrowIfNull(manifests);

        foreach (var local in manifests)
        {
            // check if the manifest has a matching template in the workspace
            var remote = templates.SingleOrDefault(t => string.Equals(t.Alias, local.Alias, StringComparison.OrdinalIgnoreCase));
            if (remote is null)
            {
                logger.LogDebug("Template with alias {Alias} does not exist on the server. It will be created.", local.Alias);
                local.ChangeType = TemplateChangeType.Added;
                continue;
            }

            local.Id = remote.Id;
            local.ChangeType = HasChanged(logger, remote, local) ? TemplateChangeType.Modified : TemplateChangeType.Unmodified;
            logger.LogDebug("Template with alias {Alias} has {Suffix}.", local.Alias, local.ChangeType is TemplateChangeType.Modified ? "changed" : "not changed");
        }
    }

    private static bool HasChanged(ILogger logger, MessageTemplate remote, TemplateManifest local)
    {
        // check if the default body changed
        var bodyChanged = !string.Equals(remote.Body, local.Body, StringComparison.InvariantCulture);

        // check if translations changed (either it is null or the counts are different)
        var translationsChanged = remote.Translations is null && local.Translations is not null
                               || remote.Translations is not null && local.Translations is null
                               || remote.Translations?.Count != local.Translations?.Count;
        if (!translationsChanged && remote.Translations is not null && local.Translations is not null)
        {
            // if a key does not exist or the body does not match, it changed
            foreach (var kvp in local.Translations)
            {
                if (!remote.Translations.TryGetValue(kvp.Key, out var translation)
                    || !string.Equals(kvp.Value, translation.Body, StringComparison.InvariantCulture))
                {
                    translationsChanged = true;
                    break;
                }
            }
        }

        // check if description changed
        var descriptionChanged = !string.Equals(remote.Description, local.Info.Description, StringComparison.InvariantCulture);

        // check if metadata changed (either it is null or the counts are different)
        var metadataChanged = remote.Metadata is null && local.Info.Metadata is not null
                           || remote.Metadata is not null && local.Info.Metadata is null
                           || remote.Metadata?.Count != local.Info.Metadata?.Count;
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

        logger.LogDebug("Checked for changes on template alias '{Alias}'."
                      + "\r\nBody:{bodyChanged}, Translations:{translationsChanged}, Description:{descriptionChanged}, Metadata:{metadataChanged}",
                        remote.Alias,
                        bodyChanged,
                        translationsChanged,
                        descriptionChanged,
                        metadataChanged);
        return bodyChanged || translationsChanged || descriptionChanged || metadataChanged;
    }

    private static async Task<IReadOnlyList<TemplateManifest>> ReadManifestsAsync(ILogger logger, string templatesDirectory, CancellationToken cancellationToken)
    {
        var results = new List<TemplateManifest>();
        var directories = Directory.EnumerateDirectories(templatesDirectory);
        foreach (var dirPath in directories)
        {
            // there is no info file, we skip the folder/directory
            var infoPath = Path.Combine(dirPath, TemplateConstants.InfoFileName);
            if (!File.Exists(infoPath))
            {
                logger.LogDebug("Skipping directory at {Directory} because it does not have an info file", dirPath);
                continue;
            }

            logger.LogDebug("Reading manifest from {Directory}", dirPath);

            // read the info
            using var stream = File.OpenRead(infoPath);
            var info = await JsonSerializer.DeserializeAsync(stream, FaluCliJsonSerializerContext.Default.TemplateInfo, cancellationToken)
                    ?? throw new InvalidOperationException($"Could not read template info from {infoPath}.");

            // read default content
            var contentPath = Path.Combine(dirPath, TemplateConstants.DefaultBodyFileName);
            var body = await ReadFromFileAsync(logger, contentPath, cancellationToken);

            // read translations
            var translations = new Dictionary<string, string>();
            var files = Directory.EnumerateFiles(dirPath);
            foreach (var file in files)
            {
                var match = TemplateConstants.TranslatedBodyFileNamePattern.Match(file);
                if (!match.Success) continue;

                contentPath = file;
                var translated = await ReadFromFileAsync(logger, contentPath, cancellationToken);
                var language = match.Groups[1].Value;
                translations[language] = translated;
            }

            results.Add(new TemplateManifest(info, body, translations));
        }

        return results;
    }

    private static async Task<string> ReadFromFileAsync(ILogger logger, string path, CancellationToken cancellationToken)
    {
        logger.LogDebug("Reading file at {Path}", path);
        using var stream = File.OpenRead(path);
        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync(cancellationToken);
    }
}
