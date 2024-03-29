﻿using Res = Falu.Properties.Resources;

namespace Falu.Commands.Messages;

public abstract class AsbtractMessagesSendCommand : Command
{
    public AsbtractMessagesSendCommand(string name, string? description = null) : base(name, description)
    {
        this.AddOption<string[]>(["--to", "-t"],
                                 description: "Phone number(s) you are sending to, in E.164 format.",
                                 validate: (or) => or.ErrorMessage = ValidateNumbers(or.Option.Name, or.GetValueOrDefault<string[]>()!));

        this.AddOption<string>(["-f", "--file"],
                               description: "File path for the path containing the phone numbers you are sending to, in E.164 format."
                                          + " The file should have no headers, all values on one line, separated by commas.",
                               validate: (or) =>
                               {
                                   // ensure the file exists
                                   var value = or.GetValueOrDefault<string>()!;
                                   var info = new FileInfo(value);
                                   if (!info.Exists)
                                   {
                                       or.ErrorMessage = $"The file {value} does not exist.";
                                       return;
                                   }

                                   var numbers = File.ReadAllText(value)
                                                     .Replace("\r\n", ",")
                                                     .Replace("\r", ",")
                                                     .Replace("\n", ",")
                                                     .Split(',', StringSplitOptions.RemoveEmptyEntries);
                                   or.ErrorMessage = ValidateNumbers(or.Option.Name, numbers);
                               });

        this.AddOption(["--stream", "-s"],
                       description: "The stream to use, either the name or unique identifier.\r\nExample: mstr_610010be9228355f14ce6e08 or transactional",
                       defaultValue: "transactional",
                       configure: o => o.IsRequired = true);

        this.AddOption<Uri?>(["--media-url"],
                             description: "Publicly accessible URL of the media to include in the message(s).\r\nExample: https://c1.staticflickr.com/3/2899/14341091933_1e92e62d12_b.jpg");

        this.AddOption(["--media-file-id"],
                       description: "The unique identifier of the pre-uploaded file containing the media to include in the message(s).\r\nExample: file_602a8dd0a54847479a874de4",
                       format: Constants.Iso8061DurationFormat);

        this.AddOption<DateTimeOffset?>(["--schedule-time", "--time"],
                                        description: $"The time at which the message(s) should be in the future.\r\nExample: {DateTime.Today.AddDays(1):O}");

        this.AddOption(["--schedule-delay", "--delay"],
                       description: "The delay (in ISO8601 duration format) to be applied by the server before sending the message(s).\r\nExample: PT10M for 10 minutes",
                       format: Constants.Iso8061DurationFormat);
    }

    private static string? ValidateNumbers(string optionName, string[] numbers)
    {
        // ensure not more than 1k messages in a batch
        var limit = 1_000;
        if (numbers.Length > limit)
        {
            return string.Format(Res.TooManyMessagesToBeSent, limit);
        }

        // ensure each value is in E.164 format
        foreach (var n in numbers)
        {
            if (!Constants.E164PhoneNumberFormat.IsMatch(n))
            {
                return string.Format(Res.InvalidE164PhoneNumber, optionName, n);
            }
        }

        return null;
    }
}

public class MessagesSendRawCommand : AsbtractMessagesSendCommand
{
    public MessagesSendRawCommand() : base("raw", "Send a message with the body defined.")
    {
        this.AddOption<string>(["--body"],
                               description: "The actual message content to be sent.",
                               configure: o => o.IsRequired = true);
    }
}

public class MessagesSendTemplatedCommand : AsbtractMessagesSendCommand
{
    public MessagesSendTemplatedCommand() : base("templated", "Send a templated message.")
    {
        this.AddOption(["--id", "-i"],
                       description: "The unique template identifier.\r\nExample: mtpl_610010be9228355f14ce6e08",
                       format: Constants.MessageTemplateIdFormat);

        this.AddOption(["--alias", "-a"],
                       description: "The template alias, unique to your workspace.",
                       format: Constants.MessageTemplateAliasFormat);

        this.AddOption<string>(["--language", "--lang"],
                               description: "The language or translation to use in the template. This is represented as the ISO-639-3 code.\r\nExample: swa for Swahili or fra for French");

        this.AddOption(["--model", "-m"],
                       description: "The model to use with the template.\r\nExample --model '{\"name\": \"John\"}'",
                       defaultValue: "{}",
                       validate: (or) =>
                       {
                           var value = or.GetValueOrDefault<string>()!;
                           try
                           {
                               _ = System.Text.Json.Nodes.JsonNode.Parse(value)!.AsObject();
                           }
                           catch (System.Text.Json.JsonException)
                           {
                               or.ErrorMessage = string.Format(Res.InvalidJsonInputValue, or.Option.Name);
                           }
                       },
                       configure: o => o.IsRequired = true);
    }
}
