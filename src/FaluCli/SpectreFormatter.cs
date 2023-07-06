namespace Falu;

internal static class SpectreFormatter
{
    public static string ColouredRed(string message) => Coloured("red", message);
    public static string ColouredGreen(string message) => Coloured("green", message);
    public static string Coloured(string color, object value) => $"[{color}]{value}[/]";

    public static string ForColorizedStatus(int code)
    {
        return code switch
        {
            >= 500 => Coloured("red", code),
            >= 300 => Coloured("yellow", code),
            _ => Coloured("green", code),
        };
    }

    public static string ForLink(string text, string url) => $"[link={url}]{text}[/]";

    public static string EscapeSquares(string text) => $"[[{text}]]";
}
