namespace Falu.Commands.Templates;

internal class TemplateManifest(TemplateInfo info, string body, Dictionary<string, string> translations)
{
    public TemplateInfo Info { get; } = info;

    public string? Alias => Info.Alias;

    public string Body { get; } = body ?? throw new ArgumentNullException(nameof(body));

    public Dictionary<string, string> Translations { get; set; } = translations;

    public TemplateChangeType ChangeType { get; set; } = TemplateChangeType.Unmodified;

    public string? Id { get; set; }
}

internal enum TemplateChangeType
{
    Unmodified,
    Added,
    Modified,
}
