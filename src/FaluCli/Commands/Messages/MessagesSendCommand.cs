using Falu.MessageBatches;
using Falu.Messages;
using Falu.MessageTemplates;
using System.Diagnostics.CodeAnalysis;
using Res = Falu.Properties.Resources;

namespace Falu.Commands.Messages;

internal abstract class AbstractMessagesSendCommand : WorkspacedCommand
{
    private readonly CliOption<string[]> toOption;
    private readonly CliOption<string> fileOption;
    private readonly CliOption<string> streamOption;
    private readonly CliOption<Uri?> mediaUrlOption;
    private readonly CliOption<string?> mediaFileIdOption;
    private readonly CliOption<DateTimeOffset?> scheduleTimeOption;
    private readonly CliOption<string> scheduleDelayOption;

    protected AbstractMessagesSendCommand(string name, string? description = null) : base(name, description)
    {
        toOption = new CliOption<string[]>(name: "--to", aliases: ["-t"]) { Description = "Phone number(s) you are sending to, in E.164 format.", };
        Add(toOption);

        fileOption = new CliOption<string>(name: "--file", aliases: ["-f"])
        {
            Description = "File path for the path containing the phone numbers you are sending to, in E.164 format."
                       + " The file should have no headers, all values on one line, separated by commas.",
        };
        Add(fileOption);

        streamOption = new CliOption<string>(name: "--stream", aliases: ["-s"])
        {
            Description = "The stream to use, either the name or unique identifier.\r\nExample: mstr_610010be9228355f14ce6e08 or transactional",
            DefaultValueFactory = r => "transactional",
            Required = true,
        };
        Add(streamOption);

        mediaUrlOption = new CliOption<Uri?>(name: "--media-url")
        {
            Description = "Publicly accessible URL of the media to include in the message(s).\r\nExample: https://c1.staticflickr.com/3/2899/14341091933_1e92e62d12_b.jpg",
        };
        Add(mediaUrlOption);

        mediaFileIdOption = new CliOption<string?>("--media-file-id")
        {
            Description = "The unique identifier of the pre-uploaded file containing the media to include in the message(s).\r\nExample: file_602a8dd0a54847479a874de4",
        };
        mediaFileIdOption.MatchesFormat(Constants.FileIdFormat);
        Add(mediaFileIdOption);

        scheduleTimeOption = new CliOption<DateTimeOffset?>(name: "--schedule-time", aliases: ["--time"])
        {
            Description = $"The time at which the message(s) should be in the future.\r\nExample: {DateTime.Today.AddDays(1):O}",
        };
        Add(scheduleTimeOption);

        scheduleDelayOption = new CliOption<string>(name: "--schedule-delay", aliases: ["--delay"])
        {
            Description = "The delay (in ISO8601 duration format) to be applied by the server before sending the message(s).\r\nExample: PT10M for 10 minutes",
            DefaultValueFactory = r => "PT60M",
        };
        scheduleDelayOption.IsValidDuration();
        Add(scheduleDelayOption);
    }

    public override async Task<int> ExecuteAsync(CliCommandExecutionContext context, CancellationToken cancellationToken)
    {
        // ensure both to and file are not null or empty
        var tos = context.ParseResult.GetValue(toOption);
        var filePath = context.ParseResult.GetValue(fileOption);
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

        // ensure the file exists
        if (filePath is not null && !File.Exists(filePath))
        {
            context.Logger.LogError("The file {value} does not exist.", filePath);
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

        // validate the numbers
        if (!TryValidateNumbers(tos, out var errorMessage))
        {
            context.Logger.LogError("{Message}", errorMessage);
            return -1;
        }

        var stream = context.ParseResult.GetValue(streamOption)!;

        // ensure both media URL and medial file Id are not specified
        var mediaUrl = context.ParseResult.GetValue(mediaUrlOption);
        var mediaFileId = context.ParseResult.GetValue(mediaFileIdOption);
        if (mediaUrl is not null && mediaFileId is not null)
        {
            context.Logger.LogError("Media URL and File ID cannot be specified together.");
            return -1;
        }

        var media = mediaUrl is not null || mediaFileId is not null
                  ? new[] { new MessageCreateOptionsMedia { Url = mediaUrl?.ToString(), File = mediaFileId, }, }
                  : null;

        // ensure both time and delay are not specified
        var time = context.ParseResult.GetValue(scheduleTimeOption);
        var delay = context.ParseResult.GetValue(scheduleDelayOption);
        if (time is not null && delay is not null)
        {
            context.Logger.LogError("Schedule time and delay cannot be specified together.");
            return -1;
        }

        // make the schedule
        var schedule = time is not null
                     ? (MessageCreateOptionsSchedule)time
                     : delay is not null
                        ? (MessageCreateOptionsSchedule)delay
                        : null;

        // if there is only a single number, send a single message, otherwise use the batch
        if (tos.Length == 1)
        {
            var target = tos[0];
            var options = new MessageCreateOptions { To = target, Stream = stream, Media = media, Schedule = schedule, };
            PopulateRequest(context, options);
            var rr = await context.Client.Messages.CreateAsync(options, cancellationToken: cancellationToken);
            rr.EnsureSuccess();

            var response = rr.Resource!;
            context.Logger.LogInformation("Scheduled {MessageId} for sending at {Scheduled:f}.", response.Id, (response.Schedule?.Time ?? response.Created).ToLocalTime());
        }
        else
        {
            var message = new MessageBatchCreateOptionsMessage { Tos = tos, Media = media, };
            PopulateRequest(context, message);
            var options = new MessageBatchCreateOptions { Messages = [message], Stream = stream, Schedule = schedule, };
            var rr = await context.Client.MessageBatches.CreateAsync(options, cancellationToken: cancellationToken);
            rr.EnsureSuccess();

            var response = rr.Resource!;
            var ids = response.Messages!;
            context.Logger.LogInformation("Scheduled {Count} messages for sending at {Scheduled:f}.", ids.Count, (response.Schedule?.Time ?? response.Created).ToLocalTime());
            context.Logger.LogDebug("Message Id(s):\r\n- {Ids}", string.Join("\r\n- ", ids));
        }

        return 0;
    }

    protected abstract void PopulateRequest(CliCommandExecutionContext context, MessageCreateOptions options);
    protected abstract void PopulateRequest(CliCommandExecutionContext context, MessageBatchCreateOptionsMessage options);

    private static bool TryValidateNumbers(string[] numbers, [NotNullWhen(false)] out string? errorMessage)
    {
        // ensure not more than 1k messages in a batch
        const int limit = 1_000;
        if (numbers.Length > limit)
        {
            errorMessage = string.Format(Res.TooManyMessagesToBeSent, limit);
            return false;
        }

        // ensure each value is in E.164 format
        foreach (var n in numbers)
        {
            if (!Constants.E164PhoneNumberFormat.IsMatch(n))
            {
                errorMessage = string.Format(Res.InvalidE164PhoneNumber, n);
                return false;
            }
        }

        errorMessage = null;
        return true;
    }
}

internal class MessagesSendRawCommand : AbstractMessagesSendCommand
{
    private readonly CliOption<string> bodyOption;

    public MessagesSendRawCommand() : base("raw", "Send a message with the body defined.")
    {
        bodyOption = new CliOption<string>(name: "--body") { Description = "The actual message content to be sent.", Required = true, };
        Add(bodyOption);
    }

    protected override void PopulateRequest(CliCommandExecutionContext context, MessageCreateOptions options) => options.Body = GetBody(context);
    protected override void PopulateRequest(CliCommandExecutionContext context, MessageBatchCreateOptionsMessage options) => options.Body = GetBody(context);
    private string? GetBody(CliCommandExecutionContext context) => context.ParseResult.GetValue(bodyOption);
}

internal class MessagesSendTemplatedCommand : AbstractMessagesSendCommand
{
    private readonly CliOption<string> idOption;
    private readonly CliOption<string> aliasOption;
    private readonly CliOption<string> languageOption;
    private readonly CliOption<string> modelOption;

    public MessagesSendTemplatedCommand() : base("templated", "Send a templated message.")
    {
        idOption = new CliOption<string>(name: "--id", aliases: ["-i"])
        {
            Description = "The unique template identifier.\r\nExample: mtpl_610010be9228355f14ce6e08",
        };
        idOption.MatchesFormat(Constants.MessageTemplateIdFormat);
        Add(idOption);

        aliasOption = new CliOption<string>(name: "--alias", aliases: ["-a"])
        {
            Description = "The template alias, unique to your workspace.",
        };
        aliasOption.MatchesFormat(Constants.MessageTemplateAliasFormat);
        Add(aliasOption);

        languageOption = new CliOption<string>(name: "--language", aliases: ["--lang"])
        {
            Description = "The language or translation to use in the template. This is represented as the ISO-639-3 code.\r\nExample: swa for Swahili or fra for French",
        };
        Add(languageOption);

        modelOption = new CliOption<string>(name: "--model", aliases: ["-m"])
        {
            Description = "The model to use with the template.\r\nExample --model '{\"name\": \"John\"}'",
            DefaultValueFactory = r => "{}",
            Required = true,
        };
        Add(modelOption);
    }

    public override Task<int> ExecuteAsync(CliCommandExecutionContext context, CancellationToken cancellationToken)
    {
        // ensure both id and alias are not null
        var id = context.ParseResult.GetValue(idOption);
        var alias = context.ParseResult.GetValue(aliasOption);
        if (string.IsNullOrWhiteSpace(id) && string.IsNullOrWhiteSpace(alias))
        {
            context.Logger.LogError("A template identifier or template alias must be provided when sending a templated message.");
            return Task.FromResult(-1);
        }

        // ensure both id and alias are not specified
        if (!string.IsNullOrWhiteSpace(id) && !string.IsNullOrWhiteSpace(alias))
        {
            context.Logger.LogError("Either specify the template identifier or template alias not both.");
            return Task.FromResult(-1);
        }

        // ensure the model is a valid JSON
        var modelJson = context.ParseResult.GetValue(modelOption)!;
        try
        {
            _ = System.Text.Json.Nodes.JsonNode.Parse(modelJson)!.AsObject();
        }
        catch (System.Text.Json.JsonException)
        {
            context.Logger.LogError("{Message}", string.Format(Res.InvalidJsonInputValue, modelOption.Name));
            return Task.FromResult(-1);
        }

        return base.ExecuteAsync(context, cancellationToken);
    }

    protected override void PopulateRequest(CliCommandExecutionContext context, MessageCreateOptions options) => options.Template = GetTemplate(context);
    protected override void PopulateRequest(CliCommandExecutionContext context, MessageBatchCreateOptionsMessage options) => options.Template = GetTemplate(context);
    private MessageCreateOptionsTemplate? GetTemplate(CliCommandExecutionContext context)
    {
        var id = context.ParseResult.GetValue(idOption);
        var alias = context.ParseResult.GetValue(aliasOption);
        var language = context.ParseResult.GetValue(languageOption);
        var modelJson = context.ParseResult.GetValue(modelOption)!;
        var model = new MessageTemplateModel(System.Text.Json.Nodes.JsonNode.Parse(modelJson)!.AsObject());

        return new MessageCreateOptionsTemplate { Id = id, Alias = alias, Language = language, Model = model, };
    }
}
