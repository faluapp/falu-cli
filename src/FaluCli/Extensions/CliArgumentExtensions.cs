using System.Text.RegularExpressions;
using Tingle.Extensions.Primitives;
using Res = Falu.Properties.Resources;

namespace System.CommandLine;

internal static class CliArgumentExtensions
{
    /// <summary>Function that gets the error message for a validation error.</summary>
    /// <typeparam name="TResult"></typeparam>
    /// <param name="value">The value that was validated.</param>
    /// <param name="result"></param>
    /// <returns>A message to be used as the error message.</returns>
    public delegate string ErrorGetter<TResult>(string? value, TResult result) where TResult : SymbolResult;

    public static void MatchesFormat<T>(this CliArgument<T> argument, Regex format)
    {
        argument.Validators.Add(Validator);
        void Validator(ArgumentResult ar) => MatchesFormat(ar, format, nulls: false, (r) => r.Argument.Name);
    }
    public static void MatchesFormat<T>(this CliOption<T> argument, Regex format, bool nulls = false)
    {
        argument.Validators.Add(Validator);
        void Validator(OptionResult ar) => MatchesFormat(ar, format, nulls, (r) => r.Option.Name);
    }
    public static void MatchesFormat<T>(this CliArgument<T> argument, Regex format, ErrorGetter<ArgumentResult> errorGetter)
    {
        argument.Validators.Add(Validator);
        void Validator(ArgumentResult ar) => MatchesFormat(ar, format, nulls: false, errorGetter);
    }
    public static void MatchesFormat<T>(this CliOption<T> argument, Regex format, bool nulls, ErrorGetter<OptionResult> errorGetter)
    {
        argument.Validators.Add(Validator);
        void Validator(OptionResult ar) => MatchesFormat(ar, format, nulls, errorGetter);
    }
    static void MatchesFormat<TResult>(TResult result, Regex format, bool nulls, Func<TResult, string> nameGetter) where TResult : SymbolResult
        => MatchesFormat(result, format, nulls, (v, r) => string.Format(Res.InvalidInputValue, nameGetter(r), format));
    static void MatchesFormat<TResult>(TResult result, Regex format, bool nulls, ErrorGetter<TResult> errorGetter) where TResult : SymbolResult
    {
        // Cannot use GetValueOrDefault<T>() because it calls all the validators
        for (var i = 0; i < result.Tokens.Count; i++)
        {
            var token = result.Tokens[i];

            // TODO: figure out if we need the line below which contains private methods
            //if (token.Symbol is not null && token.Symbol != argument) continue;
            var value = token.Value;
            if (nulls && value is null) continue;
            if (value is null || !format.IsMatch(value))
            {
                result.AddError(errorGetter(value, result));
            }
        }
    }

    public static void IsValidDuration<T>(this CliArgument<T> argument)
    {
        argument.Validators.Add(Validator);
        static void Validator(ArgumentResult ar) => IsValidDuration(ar, nulls: false);
    }
    public static void IsValidDuration<T>(this CliOption<T> argument, bool nulls = false)
    {
        argument.Validators.Add(Validator);
        void Validator(OptionResult ar) => IsValidDuration(ar, nulls);
    }
    static void IsValidDuration<TResult>(TResult result, bool nulls) where TResult : SymbolResult
        => IsValidDuration(result, nulls, (v, r) => string.Format(Res.InvalidDurationValue, v));
    static void IsValidDuration<TResult>(TResult result, bool nulls, ErrorGetter<TResult> errorGetter) where TResult : SymbolResult
    {
        // Cannot use GetValueOrDefault<T>() because it calls all the validators
        for (var i = 0; i < result.Tokens.Count; i++)
        {
            var token = result.Tokens[i];

            // TODO: figure out if we need the line below which contains private methods
            //if (token.Symbol is not null && token.Symbol != argument) continue;
            var value = token.Value;
            if (nulls && value is null) continue;
            if (value is null || !Duration.TryParse(value, out _))
            {
                result.AddError(errorGetter(value, result));
            }
        }
    }

    public static void IsWithRange<T>(this CliArgument<T> argument, T min, T max) where T : IComparable, IParsable<T>
    {
        argument.Validators.Add(Validator);
        void Validator(ArgumentResult ar) => IsWithRange(ar, min, max, nulls: false);
    }
    public static void IsWithRange<T>(this CliOption<T> argument, T min, T max, bool nulls = false) where T : IComparable, IParsable<T>
    {
        argument.Validators.Add(Validator);
        void Validator(OptionResult ar) => IsWithRange(ar, min, max, nulls);
    }
    public static void IsWithRange<T>(this CliArgument<T> argument, T min, T max, ErrorGetter<ArgumentResult> errorGetter) where T : IComparable, IParsable<T>
    {
        argument.Validators.Add(Validator);
        void Validator(ArgumentResult ar) => IsWithRange(ar, min, max, nulls: false, errorGetter);
    }
    public static void IsWithRange<T>(this CliOption<T> argument, T min, T max, bool nulls, ErrorGetter<OptionResult> errorGetter) where T : IComparable, IParsable<T>
    {
        argument.Validators.Add(Validator);
        void Validator(OptionResult ar) => IsWithRange(ar, min, max, nulls, errorGetter);
    }
    public static void IsWithRange<T>(this CliArgument<T[]> argument, T min, T max) where T : IComparable, IParsable<T>
    {
        argument.Validators.Add(Validator);
        void Validator(ArgumentResult ar) => IsWithRange(ar, min, max, nulls: false);
    }
    public static void IsWithRange<T>(this CliOption<T[]> argument, T min, T max, bool nulls = false) where T : IComparable, IParsable<T>
    {
        argument.Validators.Add(Validator);
        void Validator(OptionResult ar) => IsWithRange(ar, min, max, nulls);
    }
    public static void IsWithRange<T>(this CliArgument<T[]> argument, T min, T max, ErrorGetter<ArgumentResult> errorGetter) where T : IComparable, IParsable<T>
    {
        argument.Validators.Add(Validator);
        void Validator(ArgumentResult ar) => IsWithRange(ar, min, max, nulls: false, errorGetter);
    }
    public static void IsWithRange<T>(this CliOption<T[]> argument, T min, T max, bool nulls, ErrorGetter<OptionResult> errorGetter) where T : IComparable, IParsable<T>
    {
        argument.Validators.Add(Validator);
        void Validator(OptionResult ar) => IsWithRange(ar, min, max, nulls, errorGetter);
    }
    static void IsWithRange<TResult, T>(TResult result, T min, T max, bool nulls) where TResult : SymbolResult where T : IComparable, IParsable<T>
        => IsWithRange(result, min, max, nulls, (v, r) => string.Format(Res.InvalidDurationValue, v));
    static void IsWithRange<TResult, T>(TResult result, T min, T max, bool nulls, ErrorGetter<TResult> errorGetter) where TResult : SymbolResult where T : IComparable, IParsable<T>
    {
        // Cannot use GetValueOrDefault<T>() because it calls all the validators
        for (var i = 0; i < result.Tokens.Count; i++)
        {
            var token = result.Tokens[i];

            // TODO: figure out if we need the line below which contains private methods
            //if (token.Symbol is not null && token.Symbol != argument) continue;
            var value = token.Value;
            if (nulls && value is null) continue;

            // CompareTo returns:
            // - Less than zero – This instance precedes obj in the sort order.
            // - Zero – This instance occurs in the same position in the sort order as obj.
            // - Greater than zero – This instance follows obj in the sort order.
            if (value is null || !T.TryParse(value, null, out var parsed) || parsed.CompareTo(min) < 0 || parsed.CompareTo(max) > 0)
            {
                result.AddError(errorGetter(value, result));
            }
        }
    }
}
