using System.Text.RegularExpressions;

namespace Falu.Commands.Templates;

internal static partial class TemplateConstants
{
    public const string InfoFileName = "info.json";
    public const string DefaultBodyFileName = "content.txt";
    public const string TranslatedBodyFileNameFormat = "content-{0}.txt";
    public static readonly Regex TranslatedBodyFileNamePattern = GetTranslatedBodyFileNamePattern();


    [GeneratedRegex("content-([a-zA-Z0-9]{3}).txt")]
    private static partial Regex GetTranslatedBodyFileNamePattern();
}
