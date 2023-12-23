using Falu.Client;
using Falu.MessageBatches;
using Falu.Messages;
using Falu.MessageTemplates;

namespace Falu.Commands.Messages;

internal class MessagesSendCommandHandler(FaluCliClient client, ILogger<MessagesSendCommandHandler> logger) : ICommandHandler
{
    int ICommandHandler.Invoke(InvocationContext context) => throw new NotImplementedException();

    public async Task<int> InvokeAsync(InvocationContext context)
    {
        // ensure both to and file are not null or empty
        var tos = context.ParseResult.ValueForOption<string[]>("--to");
        var filePath = context.ParseResult.ValueForOption<string>("--file");
        if ((tos is null || tos.Length == 0) && string.IsNullOrWhiteSpace(filePath))
        {
            logger.LogError("A CSV file path must be specified or the destinations using the --to option.");
            return -1;
        }

        // ensure both to and file are not specified
        if (tos is not null && tos.Length > 0 && !string.IsNullOrWhiteSpace(filePath))
        {
            logger.LogError("Either specify the CSV file path or destinations not both.");
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
            logger.LogError("Media URL and File ID cannot be specified together.");
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
            logger.LogError("Schedule time and delay cannot be specified together.");
            return -1;
        }

        // make the schedule
        var schedule = time is not null
                     ? (MessageCreateRequestSchedule)time
                     : delay is not null
                        ? (MessageCreateRequestSchedule)delay
                        : null;

        var cancellationToken = context.GetCancellationToken();

        var command = context.ParseResult.CommandResult.Command;
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
                logger.LogError("A template identifier or template alias must be provided when sending a templated message.");
                return -1;
            }

            // ensure both id and alias are not specified
            if (!string.IsNullOrWhiteSpace(id) && !string.IsNullOrWhiteSpace(alias))
            {
                logger.LogError("Either specify the template identifier or template alias not both.");
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
            var rr = await client.Messages.CreateAsync(request, cancellationToken: cancellationToken);
            rr.EnsureSuccess();

            var response = rr.Resource!;
            logger.LogInformation("Scheduled {MessageId} for sending at {Scheduled:f}.", response.Id, (response.Schedule?.Time ?? response.Created).ToLocalTime());
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
            var rr = await client.MessageBatches.CreateAsync(request, cancellationToken: cancellationToken);
            rr.EnsureSuccess();

            var response = rr.Resource!;
            var ids = response.Messages!;
            logger.LogInformation("Scheduled {Count} messages for sending at {Scheduled:f}.", ids.Count, (response.Schedule?.Time ?? response.Created).ToLocalTime());
            logger.LogDebug("Message Id(s):\r\n- {Ids}", string.Join("\r\n- ", ids));
        }

        return 0;
    }
}
