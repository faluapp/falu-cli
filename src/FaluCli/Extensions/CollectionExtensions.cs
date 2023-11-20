namespace System.Collections.Generic;

internal static class CollectionExtensions
{
    public static T[]? NullIfEmpty<T>(this T[]? source) => source is null || source.Length == 0 ? null : source;

    public static string MakePaddedString(this IDictionary<string, object> dictionary, out int width, string separator = " : ")
    {
        ArgumentNullException.ThrowIfNull(dictionary);

        if (dictionary.Count == 0)
        {
            width = 0;
            return string.Empty;
        }

        var totalWidth = width = dictionary.Max(kvp => kvp.Key.Length);
        return string.Join("\r\n", dictionary.Select(kvp => $"{kvp.Key.PadRight(totalWidth)}{separator}{kvp.Value}"));
    }

    public static string MakePaddedString(this IDictionary<string, object> dictionary, string separator = " : ")
    {
        return dictionary.MakePaddedString(out _, separator);
    }

    public static string MakeString(this IDictionary<string, object> dictionary, string separator = " : ")
    {
        ArgumentNullException.ThrowIfNull(dictionary);

        if (dictionary.Count == 0)
        {
            return string.Empty;
        }

        return string.Join("\r\n", dictionary.Select(kvp => $"{kvp.Key}{separator}{kvp.Value}"));
    }

    public static Dictionary<string, object> RemoveDefaultAndEmpty(this IDictionary<string, object?> data)
    {
        var result = new Dictionary<string, object>();

        foreach (var kvp in data)
        {
            switch (kvp.Value)
            {
                // Remove default and empty values
                case string s when string.IsNullOrWhiteSpace(s):
                case DateTimeOffset dto when dto == default:
                case DateTime dt when dt == default:
                case TimeSpan ts when ts == default:
                case long l when l == default:
                case null:
                    continue; // nothing more

                case DateTimeOffset dto:
                    result[kvp.Key] = dto.ToString("R");
                    continue;
                case DateTime dt:
                    result[kvp.Key] = dt.ToString("R");
                    continue;
                case TimeSpan ts:
                    result[kvp.Key] = ts.ToReadableString();
                    continue;
                default:
                    result.Add(kvp.Key, kvp.Value);
                    continue;
            }
        }

        return result;
    }

    internal static string ToReadableString(this TimeSpan span)
    {
        string formatted = string.Format("{0}{1}{2}{3}",
                                         (span.Days / 7) > 0 ? $"{(span.Days / 7):0} weeks, " : string.Empty,
                                         span.Days % 7 > 0 ? $"{(span.Days % 7):0} days, " : string.Empty,
                                         span.Hours > 0 ? $"{span.Hours:0} hours, " : string.Empty,
                                         span.Minutes > 0 ? $"{span.Minutes:0} min, " : string.Empty);

        if (formatted.EndsWith(", ")) formatted = formatted[0..^2];
        return formatted;
    }
}
