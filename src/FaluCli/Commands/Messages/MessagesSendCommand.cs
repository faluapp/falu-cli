using Falu.MessageBatches;
using Falu.Messages;
using Falu.MessageTemplates;
using Res = Falu.Properties.Resources;

namespace Falu.Commands.Messages;

internal abstract class AbstractMessagesSendCommand : WorkspacedCommand
{
    protected AbstractMessagesSendCommand(string name, string? description = null) : base(name, description)
    {
        this.AddOption<string[]>(["--to", "-t"],
                                 description: "Phone number(s) you are sending to, in E.164 format.",
                                 validate: (or) =>
                                 {
                                     var em = ValidateNumbers(or.Option.Name, or.GetValueOrDefault<string[]>()!);
                                     if (em is not null) or.AddError(em);
                                 });

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
                                       or.AddError($"The file {value} does not exist.");
                                       return;
                                   }

                                   var numbers = File.ReadAllText(value)
                                                     .Replace("\r\n", ",")
                                                     .Replace("\r", ",")
                                                     .Replace("\n", ",")
                                                     .Split(',', StringSplitOptions.RemoveEmptyEntries);
                                   var em = ValidateNumbers(or.Option.Name, numbers);
                                   if (em is not null) or.AddError(em);
                               });

        this.AddOption(["--stream", "-s"],
                       description: "The stream to use, either the name or unique identifier.\r\nExample: mstr_610010be9228355f14ce6e08 or transactional",
                       defaultValue: "transactional",
                       configure: o => o.Required = true);

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
        const int limit = 1_000;
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

    public override async Task<int> ExecuteAsync(CliCommandExecutionContext context, CancellationToken cancellationToken)
    {
        var command = context.ParseResult.CommandResult.Command;

        // ensure both to and file are not null or empty
        var tos = context.ParseResult.ValueForOption<string[]>("--to");
        var filePath = context.ParseResult.ValueForOption<string>("--file");
        if ((tos is null || tos.Length == 0) && string.IsNullOrWhiteSpace(filePath))
        {
            context.Logger.LogError("A CSV file path must be specified or the destinations using the --to option.");
            return -1;
        }

        // ensure both to and file are not specified
        if (tos is not null && tos.Length > 0 && !string.IsNullOrWhiteSpace(filePath))
        {
            context.Logger.LogError("Either specify the CSV file path or destinations not both.");
            return -1;
        }

        // read the numbers from the CSV file
        if (tos is null || tos.Length == 0)
        {
            tos = File.ReadAllText(filePath!)
                      .Replace("\r\n", ",")
                      .Replace("\r", ",")
                      .Replace("\n", ",")
                      .Split(',', StringSplitOptions.RemoveEmptyEntries);
        }

        var stream = context.ParseResult.ValueForOption<string>("--stream")!;

        // ensure both media URL and medial file Id are not specified
        var mediaUrl = context.ParseResult.ValueForOption<Uri?>("--media-url");
        var mediaFileId = context.ParseResult.ValueForOption<string?>("--media-file-id");
        if (mediaUrl is not null && mediaFileId is not null)
        {
            context.Logger.LogError("Media URL and File ID cannot be specified together.");
            return -1;
        }

        var media = mediaUrl is not null || mediaFileId is not null
                  ? new[] { new MessageCreateRequestMedia { Url = mediaUrl?.ToString(), File = mediaFileId, }, }
                  : null;

        // ensure both time and delay are not specified
        var time = context.ParseResult.ValueForOption<DateTimeOffset?>("--schedule-time");
        var delay = context.ParseResult.ValueForOption<string?>("--schedule-delay");
        if (time is not null && delay is not null)
        {
            context.Logger.LogError("Schedule time and delay cannot be specified together.");
            return -1;
        }

        // make the schedule
        var schedule = time is not null
                     ? (MessageCreateRequestSchedule)time
                     : delay is not null
                        ? (MessageCreateRequestSchedule)delay
                        : null;

        string? body = null;
        MessageCreateRequestTemplate? template = null;
        if (command is MessagesSendRawCommand)
        {
            body = context.ParseResult.ValueForOption<string>("--body")!; // marked required in the command
        }
        else if (command is MessagesSendTemplatedCommand)
        {
            var id = context.ParseResult.ValueForOption<string>("--id");
            var alias = context.ParseResult.ValueForOption<string>("--alias");
            var language = context.ParseResult.ValueForOption<string>("--language");
            var modelJson = context.ParseResult.ValueForOption<string>("--model")!; // marked required in the command
            var model = new MessageTemplateModel(System.Text.Json.Nodes.JsonNode.Parse(modelJson)!.AsObject());

            // ensure both id and alias are not null
            if (string.IsNullOrWhiteSpace(id) && string.IsNullOrWhiteSpace(alias))
            {
                context.Logger.LogError("A template identifier or template alias must be provided when sending a templated message.");
                return -1;
            }

            // ensure both id and alias are not specified
            if (!string.IsNullOrWhiteSpace(id) && !string.IsNullOrWhiteSpace(alias))
            {
                context.Logger.LogError("Either specify the template identifier or template alias not both.");
                return -1;
            }

            template = new MessageCreateRequestTemplate { Id = id, Alias = alias, Language = language, Model = model, };
        }
        else throw new InvalidOperationException($"Command of type '{command.GetType().FullName}' is not supported here.");

        // if there is only a single number, send a single message, otherwise use the batch
        if (tos.Length == 1)
        {
            var target = tos[0];
            var request = new MessageCreateRequest
            {
                To = target,
                Body = body,
                Template = template,
                Stream = stream,
                Media = media,
                Schedule = schedule,
            };
            var rr = await context.Client.Messages.CreateAsync(request, cancellationToken: cancellationToken);
            rr.EnsureSuccess();

            var response = rr.Resource!;
            context.Logger.LogInformation("Scheduled {MessageId} for sending at {Scheduled:f}.", response.Id, (response.Schedule?.Time ?? response.Created).ToLocalTime());
        }
        else
        {
            var request = new MessageBatchCreateRequest
            {
                Messages =
                [
                    new MessageBatchCreateRequestMessage
                    {
                        Tos = tos,
                        Body = body,
                        Template = template,
                        Media = media,
                    },
                ],
                Stream = stream,
                Schedule = schedule,
            };
            var rr = await context.Client.MessageBatches.CreateAsync(request, cancellationToken: cancellationToken);
            rr.EnsureSuccess();

            var response = rr.Resource!;
            var ids = response.Messages!;
            context.Logger.LogInformation("Scheduled {Count} messages for sending at {Scheduled:f}.", ids.Count, (response.Schedule?.Time ?? response.Created).ToLocalTime());
            context.Logger.LogDebug("Message Id(s):\r\n- {Ids}", string.Join("\r\n- ", ids));
        }

        return 0;
    }
}

internal class MessagesSendRawCommand : AbstractMessagesSendCommand
{
    public MessagesSendRawCommand() : base("raw", "Send a message with the body defined.")
    {
        this.AddOption<string>(["--body"],
                               description: "The actual message content to be sent.",
                               configure: o => o.Required = true);
    }
}

internal class MessagesSendTemplatedCommand : AbstractMessagesSendCommand
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
                               or.AddError(string.Format(Res.InvalidJsonInputValue, or.Option.Name));
                           }
                       },
                       configure: o => o.Required = true);
    }
}
