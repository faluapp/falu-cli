namespace System.CommandLine.IO;

/// <summary>
/// Extension methods for <see cref="IConsole"/>
/// </summary>
internal static class ConsoleExtensions
{
    private static readonly bool ColorsAreSupported = GetColorsAreSupported();

    private static bool GetColorsAreSupported()
        => !(OperatingSystem.IsBrowser() || OperatingSystem.IsAndroid() || OperatingSystem.IsIOS() || OperatingSystem.IsTvOS())
        && !Console.IsOutputRedirected;

    internal static void SetTerminalForegroundRed(this IConsole console) => console.SetTerminalForegroundColor(ConsoleColor.Red);
    internal static void SetTerminalForegroundGreen(this IConsole console) => console.SetTerminalForegroundColor(ConsoleColor.Green);

    internal static void SetTerminalForegroundColor(this IConsole _, ConsoleColor color)
    {
        if (ColorsAreSupported)
        {
            Console.ForegroundColor = color;
        }
    }

    internal static void ResetTerminalForegroundColor(this IConsole _)
    {
        if (ColorsAreSupported)
        {
            Console.ResetColor();
        }
    }
}

/// <summary>
/// Extension methods for <see cref="IStandardStreamWriter"/>
/// </summary>
public static class IStandardStreamWriterExtensions
{
    public static void WriteLine(this IStandardStreamWriter writer, string format, params object?[] args)
    {
        writer.WriteLine(string.Format(format, args));
    }
}
