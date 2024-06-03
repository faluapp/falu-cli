using Falu.Client;
using System.Text.Json;

namespace Falu.Commands.Templates;

internal class TemplatesPullCommand : AbstractTemplatesCommand
{
    public TemplatesPullCommand() : base("pull", "Download templates from Falu servers to your local file system.")
    {
        this.AddArgument<string>(name: "output-directory",
                                 description: "The directory into which to put the pulled templates.");

        this.AddOption(["-o", "--overwrite"],
                       description: "Overwrite templates if they already exist.",
                       defaultValue: false);

        this.SetHandler(HandleAsync);
    }

    private static async Task HandleAsync(InvocationContext context)
    {
        var cancellationToken = context.GetCancellationToken();
        var client = context.GetRequiredService<FaluCliClient>();
        var logger = context.GetRequiredService<ILogger<TemplatesPullCommand>>();

        async Task WriteToFileAsync(string path, bool overwrite, BinaryData data)
        {
            var exists = File.Exists(path);
            if (exists && !overwrite)
            {
                logger.LogWarning("Skipping overwrite for {Path}", path);
                return;
            }

            // delete existing file
            if (exists) File.Delete(path);

            // write to file
            logger.LogDebug("Writing to file at {Path}", path);
            await using var stream = data.ToStream();
            await using var fs = File.OpenWrite(path);
            await stream.CopyToAsync(fs, cancellationToken);
        }

        var outputPath = context.ParseResult.ValueForArgument<string>("output-directory")!;
        var overwrite = context.ParseResult.ValueForOption<bool>("--overwrite");

        // download the templates
        var templates = await DownloadTemplatesAsync(client, logger, cancellationToken);

        // work on each template
        var saved = 0;
        foreach (var template in templates)
        {
            if (string.IsNullOrWhiteSpace(template.Alias))
            {
                logger.LogWarning("Template '{TemplateId}' without an alias shall be skipped.", template.Id);
                continue;
            }

            // create directory if it does not exist
            var dirPath = Path.Combine(outputPath, template.Alias!);
            if (!Directory.Exists(dirPath)) Directory.CreateDirectory(dirPath);

            // write the default body
            var contentPath = Path.Combine(dirPath, TemplateConstants.DefaultBodyFileName);
            await WriteToFileAsync(contentPath, overwrite, BinaryData.FromString(template.Body!));

            // write the translations
            foreach (var (language, translation) in template.Translations)
            {
                contentPath = Path.Combine(dirPath, string.Format(TemplateConstants.TranslatedBodyFileNameFormat, language));
                await WriteToFileAsync(contentPath, overwrite, BinaryData.FromString(translation.Body!));
            }

            // write the template info
            var infoPath = Path.Combine(dirPath, TemplateConstants.InfoFileName);
            var info = new TemplateInfo(template);
            using var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, info, FaluCliJsonSerializerContext.Default.TemplateInfo, cancellationToken);
            stream.Seek(0, SeekOrigin.Begin);
            await WriteToFileAsync(infoPath, overwrite, await BinaryData.FromStreamAsync(stream, cancellationToken));
            saved++;
        }

        logger.LogInformation("Finished saving {Save} of {Total} templates to {OutputDirectory}", saved, templates.Count, outputPath);
    }
}
