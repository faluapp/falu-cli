using System.Text.RegularExpressions;
using Res = Falu.Properties.Resources;

namespace System.CommandLine;

/// <summary>Extension methods on <see cref="CliCommand"/>.</summary>
public static class CliCommandExtensions
{
    #region Options

    ///
    public static CliCommand AddOption(this CliCommand command,
                                       IEnumerable<string> aliases,
                                       string? description = null,
                                       Regex? format = null,
                                       Action<CliOption<string>>? configure = null)
    {
        Action<OptionResult>? validate = null;
        if (format is not null)
        {
            validate = (or) =>
            {
                var value = or.GetValueOrDefault<string>();
                if (value is null || !format.IsMatch(value))
                {
                    or.AddError(string.Format(Res.InvalidInputValue, or.Option.Name, format));
                }
            };
        }
        return command.AddOption(aliases, description, validate, configure);
    }

    ///
    public static CliCommand AddOption<T>(this CliCommand command,
                                          IEnumerable<string> aliases,
                                          string? description = null,
                                          Action<OptionResult>? validate = null,
                                          Action<CliOption<T>>? configure = null)
    {
        // Create the option and add it to the command
        var option = CreateOption(aliases: aliases,
                                  description: description,
                                  validate: validate,
                                  configure: configure);

        command.Options.Add(option);
        return command;
    }

    ///
    public static CliCommand AddOption<T>(this CliCommand command,
                                          IEnumerable<string> aliases,
                                          string? description,
                                          T defaultValue,
                                          Action<OptionResult>? validate = null,
                                          Action<CliOption<T>>? configure = null)
    {
        // Create the option and add it to the command
        var option = CreateOption(aliases: aliases,
                                  description: description,
                                  validate: validate,
                                  configure: configure);

        // Set default value
        option.DefaultValueFactory = r => defaultValue;

        command.Options.Add(option);
        return command;
    }

    private static CliOption<T> CreateOption<T>(IEnumerable<string> aliases,
                                                string? description = null,
                                                Action<OptionResult>? validate = null,
                                                Action<CliOption<T>>? configure = null)
    {
        // Create the option
        var option = new CliOption<T>(name: aliases.First(), aliases: aliases.Skip(1).ToArray()) { Description = description, };

        // Add validator if provided
        if (validate is not null)
        {
            option.Validators.Add(validate);
        }

        // Perform further configuration
        configure?.Invoke(option);

        return option;
    }

    #endregion

    #region Arguments

    ///
    public static CliCommand AddArgument(this CliCommand command,
                                         string name,
                                         string? description = null,
                                         Regex? format = null,
                                         Action<CliArgument<string>>? configure = null)
    {
        Action<ArgumentResult>? validator = null;
        if (format is not null)
        {
            validator = (ar) =>
            {
                var value = ar.GetValueOrDefault<string>();
                if (value is null || !format.IsMatch(value))
                {
                    ar.AddError(string.Format(Res.InvalidInputValue, ar.Argument.Name, format));
                }
            };
        }
        return command.AddArgument(name, description, validator, configure);
    }

    ///
    public static CliCommand AddArgument<T>(this CliCommand command,
                                            string name,
                                            string? description = null,
                                            Action<ArgumentResult>? validator = null,
                                            Action<CliArgument<T>>? configure = null)
    {
        // Create the argument and add it to the command
        var argument = CreateArgument(name: name,
                                      description: description,
                                      validator: validator,
                                      configure: configure);

        command.Arguments.Add(argument);
        return command;
    }

    ///
    public static CliCommand AddArgument<T>(this CliCommand command,
                                            string name,
                                            string? description,
                                            T defaultValue,
                                            Action<ArgumentResult>? validator = null,
                                            Action<CliArgument<T>>? configure = null)
    {
        // Create the argument and add it to the command
        var argument = CreateArgument(name: name,
                                      description: description,
                                      validator: validator,
                                      configure: configure);

        // Set default value if provided
        argument.DefaultValueFactory = r => defaultValue;

        command.Arguments.Add(argument);
        return command;
    }

    private static CliArgument<T> CreateArgument<T>(string name,
                                                    string? description = null,
                                                    Action<ArgumentResult>? validator = null,
                                                    Action<CliArgument<T>>? configure = null)
    {
        // Create the argument
        var argument = new CliArgument<T>(name: name) { Description = description, };

        // Add validator if provided
        if (validator is not null)
        {
            argument.Validators.Add(validator);
        }

        // Perform further configuration
        configure?.Invoke(argument);

        return argument;
    }

    #endregion
}
