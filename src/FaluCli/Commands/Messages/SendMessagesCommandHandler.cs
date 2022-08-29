using Falu.Client;
using Falu.MessageBatches;
using Falu.Messages;

namespace Falu.Commands.Messages;

internal class SendMessagesCommandHandler : ICommandHandler
{
    public FaluCliClient client;
    private readonly ILogger logger;

    public SendMessagesCommandHandler(FaluCliClient client, ILogger<SendMessagesCommandHandler> logger)
    {
        this.client = client ?? throw new ArgumentNullException(nameof(client));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    int ICommandHandler.Invoke(InvocationContext context) => throw new NotImplementedException();

    public Task<int> InvokeAsync(InvocationContext context)
    {
        // ensure both to and file are not null or empty
        var tos = context.ParseResult.ValueForOption<string[]>("--to");
        var filePath = context.ParseResult.ValueForOption<string>("--file");
        if ((tos is null || tos.Length == 0) && string.IsNullOrWhiteSpace(filePath))
        {
            logger.LogError("A CSV file path must be specified or the destinations using the --to option.");
            return Task.FromResult(-1);
        }

        // ensure both to and file are not specified
        if (tos is not null && tos.Length > 0 && !string.IsNullOrWhiteSpace(filePath))
        {
            logger.LogError("Either specify the CSV file path or destinations not both.");
            return Task.FromResult(-1);
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
        var cancellationToken = context.GetCancellationToken();

        var command = context.ParseResult.CommandResult.Command;
        if (command is SendRawMessagesCommand) return HandleRawAsync(context, tos, stream, cancellationToken);
        else if (command is SendTemplatedMessagesCommand) return HandleTemplatedAsync(context, tos, stream, cancellationToken);
        throw new InvalidOperationException($"Command of type '{command.GetType().FullName}' is not supported here.");
    }

    private async Task<int> HandleRawAsync(InvocationContext context, string[] tos, string stream, CancellationToken cancellationToken)
    {
        var body = context.ParseResult.ValueForOption<string>("--body");

        var messages = CreateMessages(tos, r => r.Body = body);
        await SendMessagesAsync(messages, stream, cancellationToken);
        return 0;
    }

    private async Task<int> HandleTemplatedAsync(InvocationContext context, string[] tos, string stream, CancellationToken cancellationToken)
    {
        var id = context.ParseResult.ValueForOption<string>("--id");
        var alias = context.ParseResult.ValueForOption<string>("--alias");
        var model = System.Text.Json.JsonSerializer.Deserialize<IDictionary<string, object>>(context.ParseResult.ValueForOption<string>("--model")!);

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

        var messages = CreateMessages(tos, r => r.Template = new MessageSourceTemplate { Id = id, Alias = alias, Model = model, });
        await SendMessagesAsync(messages, stream, cancellationToken);
        return 0;
    }

    private async Task SendMessagesAsync(List<MessageBatchCreateRequestMessage> messages, string stream, CancellationToken cancellationToken)
    {
        var request = new MessageBatchCreateRequest
        {
            Messages = messages,
            Stream = stream,
        };
        var rr = await client.MessageBatches.CreateAsync(request, cancellationToken: cancellationToken);
        rr.EnsureSuccess();

        var response = rr.Resource!;
        var scheduled = response.Created;
        var ids = response.Messages!;
        logger.LogInformation("Scheduled {Count} for sending at {Scheduled:r}.", ids.Count, scheduled);
        logger.LogDebug("Message Id(s):\r\n-{Ids}", string.Join("\r\n-", ids));
    }

    private static List<MessageBatchCreateRequestMessage> CreateMessages(string[] tos, Action<MessageBatchCreateRequestMessage> setupFunc)
    {
        ArgumentNullException.ThrowIfNull(tos);
        ArgumentNullException.ThrowIfNull(setupFunc);

        // a maximum of 500 tos per batch
        var groups = tos.Distinct(StringComparer.OrdinalIgnoreCase).Chunk(500).ToList();
        var messages = new List<MessageBatchCreateRequestMessage>(groups.Count);
        foreach (var group in groups)
        {
            var message = new MessageBatchCreateRequestMessage { Tos = group, };
            setupFunc(message);
            messages.Add(message);
        }
        return messages;
    }
}
