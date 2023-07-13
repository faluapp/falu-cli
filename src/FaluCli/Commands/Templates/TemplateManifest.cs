namespace Falu.Commands.Templates;

internal class TemplateManifest
{
    public TemplateManifest(TemplateInfo info, string body, Dictionary<string, string> translations)
    {
        Info = info ?? throw new ArgumentNullException(nameof(info));
        Body = body ?? throw new ArgumentNullException(nameof(body));
        Translations = translations ?? throw new ArgumentNullException(nameof(translations));
    }

    public TemplateInfo Info { get; }

    public string? Alias => Info.Alias;

    public string Body { get; }

    public Dictionary<string, string> Translations { get; set; }

    public ChangeType ChangeType { get; set; } = ChangeType.Unmodified;

    public string? Id { get; set; }
}

enum ChangeType
{
    Unmodified,
    Added,
    Modified,
}
