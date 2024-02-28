using System.Text.RegularExpressions;
using Res = Falu.Properties.Resources;

namespace System.CommandLine;

/// <summary>
/// Extension methods on <see cref="Command"/>.
/// </summary>
public static class CommandExtensions
{
    #region Options

    ///
    public static Command AddOption(this Command command,
                                    IEnumerable<string> aliases,
                                    string? description = null,
                                    Regex? format = null,
                                    Action<Option<string>>? configure = null)
    {
        ValidateSymbolResult<OptionResult>? validate = null;
        if (format is not null)
        {
            validate = (or) =>
            {
                var value = or.GetValueOrDefault<string>();
                if (value is null || !format.IsMatch(value))
                {
                    or.ErrorMessage = string.Format(Res.InvalidInputValue, or.Option.Name, format);
                }
            };
        }
        return command.AddOption(aliases, description, validate, configure);
    }

    ///
    public static Command AddOption<T>(this Command command,
                                       IEnumerable<string> aliases,
                                       string? description = null,
                                       ValidateSymbolResult<OptionResult>? validate = null,
                                       Action<Option<T>>? configure = null)
    {
        // Create the option and add it to the command
        var option = CreateOption(aliases: aliases,
                                  description: description,
                                  validate: validate,
                                  configure: configure);

        command.AddOption(option);
        return command;
    }

    ///
    public static Command AddOption<T>(this Command command,
                                       IEnumerable<string> aliases,
                                       string? description,
                                       T defaultValue,
                                       ValidateSymbolResult<OptionResult>? validate = null,
                                       Action<Option<T>>? configure = null)
    {
        // Create the option and add it to the command
        var option = CreateOption(aliases: aliases,
                                  description: description,
                                  validate: validate,
                                  configure: configure);

        // Set default value
        option.SetDefaultValue(defaultValue);

        command.AddOption(option);
        return command;
    }

    ///
    public static Command AddGlobalOption<T>(this Command command,
                                             IEnumerable<string> aliases,
                                             string? description,
                                             T defaultValue,
                                             ValidateSymbolResult<OptionResult>? validate = null,
                                             Action<Option<T>>? configure = null)
    {
        // Create the option and add it to the command
        var option = CreateOption(aliases: aliases,
                                  description: description,
                                  validate: validate,
                                  configure: configure);

        // Set default value
        option.SetDefaultValue(defaultValue);

        command.AddGlobalOption(option);
        return command;
    }

    ///
    public static Command AddGlobalOption(this Command command,
                                          IEnumerable<string> aliases,
                                          string? description = null,
                                          Regex? format = null,
                                          Action<Option<string>>? configure = null)
    {
        ValidateSymbolResult<OptionResult>? validate = null;
        if (format is not null)
        {
            validate = (or) =>
            {
                var value = or.GetValueOrDefault<string>();
                if (value is null || !format.IsMatch(value))
                {
                    or.ErrorMessage = string.Format(Res.InvalidInputValue, or.Option.Name, format);
                }
            };
        }
        return command.AddGlobalOption(aliases, description, validate, configure);
    }

    ///
    public static Command AddGlobalOption<T>(this Command command,
                                             IEnumerable<string> aliases,
                                             string? description = null,
                                             ValidateSymbolResult<OptionResult>? validate = null,
                                             Action<Option<T>>? configure = null)
    {
        // Create the option and add it to the command
        var option = CreateOption(aliases: aliases,
                                  description: description,
                                  validate: validate,
                                  configure: configure);

        command.AddGlobalOption(option);
        return command;
    }

    private static Option<T> CreateOption<T>(IEnumerable<string> aliases,
                                             string? description = null,
                                             ValidateSymbolResult<OptionResult>? validate = null,
                                             Action<Option<T>>? configure = null)
    {
        // Create the option
        var option = new Option<T>(aliases: aliases.ToArray(), description: description);

        // Add validator if provided
        if (validate is not null)
        {
            option.AddValidator(validate);
        }

        // Perform further configuration
        configure?.Invoke(option);

        return option;
    }

    #endregion

    #region Arguments

    ///
    public static Command AddArgument(this Command command,
                                      string name,
                                      string? description = null,
                                      Regex? format = null,
                                      Action<Argument<string>>? configure = null)
    {
        ValidateSymbolResult<ArgumentResult>? validate = null;
        if (format is not null)
        {
            validate = (ar) =>
            {
                var value = ar.GetValueOrDefault<string>();
                if (value is null || !format.IsMatch(value))
                {
                    ar.ErrorMessage = string.Format(Res.InvalidInputValue, ar.Argument.Name, format);
                }
            };
        }
        return command.AddArgument(name, description, validate, configure);
    }

    ///
    public static Command AddArgument<T>(this Command command,
                                         string name,
                                         string? description = null,
                                         ValidateSymbolResult<ArgumentResult>? validate = null,
                                         Action<Argument<T>>? configure = null)
    {
        // Create the argument and add it to the command
        var argument = CreateArgument(name: name,
                                      description: description,
                                      validate: validate,
                                      configure: configure);

        command.AddArgument(argument);
        return command;
    }

    ///
    public static Command AddArgument<T>(this Command command,
                                         string name,
                                         string? description,
                                         T defaultValue,
                                         ValidateSymbolResult<ArgumentResult>? validate = null,
                                         Action<Argument<T>>? configure = null)
    {
        // Create the argument and add it to the command
        var argument = CreateArgument(name: name,
                                      description: description,
                                      validate: validate,
                                      configure: configure);

        // Set default value if provided
        argument.SetDefaultValue(defaultValue);

        command.AddArgument(argument);
        return command;
    }

    private static Argument<T> CreateArgument<T>(string name,
                                                 string? description = null,
                                                 ValidateSymbolResult<ArgumentResult>? validate = null,
                                                 Action<Argument<T>>? configure = null)
    {
        // Create the argument
        var argument = new Argument<T>(name: name, description: description);

        // Add validator if provided
        if (validate is not null)
        {
            argument.AddValidator(validate);
        }

        // Perform further configuration
        configure?.Invoke(argument);

        return argument;
    }

    #endregion
}
