using Falu.Client;
using Falu.Client.Events;
using System.Net;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Falu.Commands.Events;

internal class RetryCommandHandler : ICommandHandler
{
    public FaluCliClient client;
    private readonly ILogger logger;

    public RetryCommandHandler(FaluCliClient client, ILogger<RetryCommandHandler> logger)
    {
        this.client = client ?? throw new ArgumentNullException(nameof(client));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    int ICommandHandler.Invoke(InvocationContext context) => throw new NotImplementedException();

    public async Task<int> InvokeAsync(InvocationContext context)
    {
        var cancellationToken = context.GetCancellationToken();
        var eventId = context.ParseResult.ValueForArgument<string>("event");
        var webhookEndpointId = context.ParseResult.ValueForOption<string>("--webhook-endpoint");

        var model = new EventDeliveryRetry { WebhookEndpointId = webhookEndpointId, };
        var response = await client.Events.RetryAsync(eventId!, model, cancellationToken: cancellationToken);
        response.EnsureSuccess();

        var attempt = response.Resource!;

        var time = TimeSpan.FromMilliseconds(attempt.ResponseTime);
        var data = new Dictionary<string, object?>
        {
            ["Attempted"] = attempt.Attempted,
            ["Url"] = attempt.Url,
            ["Response Time"] = $"{time.TotalSeconds:n3} seconds",
        };

        if (!attempt.Successful && context.IsVerboseEnabled())
        {
            var statusCode = Enum.Parse<HttpStatusCode>(attempt.HttpStatus.ToString());
            data["Http Status"] = $"{statusCode} ({attempt.HttpStatus})";
            if (context.IsVerboseEnabled())
            {
                data["Request Body"] = PrettyJson(attempt.RequestBody!);
                data["Response Body"] = attempt.ResponseBody;
            }
        }

        var str = $"{(attempt.Successful ? "Retry succeeded." : "Retry failed!")}\r\n";
        str += data.RemoveDefaultAndEmpty().MakePaddedString();
        logger.LogInformation("{MessageStr}", str);

        return 0;
    }

    private static string PrettyJson(string json)
    {
        var so = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        };
        return JsonNode.Parse(json)!.ToJsonString(so);
    }
}
