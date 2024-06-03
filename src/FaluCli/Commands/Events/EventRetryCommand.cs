using Falu.Client.Events;
using Spectre.Console;
using Spectre.Console.Json;
using Spectre.Console.Rendering;
using System.Net;
using System.Text;
using System.Text.Json.Nodes;

namespace Falu.Commands.Events;

internal class EventRetryCommand : WorkspacedCommand
{
    public EventRetryCommand() : base("retry", "Retry delivery of an event to a webhook endpoint.")
    {
        this.AddArgument(name: "event",
                         description: "Unique identifier of the event. Example: evt_610010be9228355f14ce6e08",
                         format: Constants.EventIdFormat);

        this.AddOption(aliases: ["--webhook-endpoint"],
                       description: "Unique identifier of the webhook endpoint. Example: we_610010be9228355f14ce6e08",
                       format: Constants.WebhookEndpointIdFormat,
                       configure: o => o.Required = true);
    }

    public override async Task<int> ExecuteAsync(CliCommandExecutionContext context, CancellationToken cancellationToken)
    {
        var eventId = context.ParseResult.ValueForArgument<string>("event");
        var webhookEndpointId = context.ParseResult.ValueForOption<string>("--webhook-endpoint");

        var model = new EventDeliveryRetry { WebhookEndpoint = webhookEndpointId, };
        var response = await context.Client.Events.RetryAsync(eventId!, model, cancellationToken: cancellationToken);
        response.EnsureSuccess();

        var attempt = response.Resource!;

        var time = TimeSpan.FromMilliseconds(attempt.ResponseTime);
        var data = new Dictionary<string, object> { ["Attempted"] = $"{attempt.Attempted:F}", ["Url"] = attempt.Url!, ["Response Time"] = $"{time.TotalSeconds:n3} seconds", };

        if (!attempt.Successful && context.ParseResult.IsVerboseEnabled())
        {
            var statusCode = Enum.Parse<HttpStatusCode>(attempt.HttpStatus.ToString());
            data["Http Status"] = $"{statusCode} ({attempt.HttpStatus})";
        }

        var sb = new StringBuilder();
        sb.AppendLine(attempt.Successful ? SpectreFormatter.ColouredGreen("Retry succeeded.") : SpectreFormatter.ColouredRed("Retry failed!"));
        sb.AppendLine(data.MakePaddedString());
        AnsiConsole.MarkupLine(sb.ToString());

        if (context.ParseResult.IsVerboseEnabled())
        {
            var requestPanel = CreatePanel(new JsonText(attempt.RequestBody!), "Request Body");
            var responseBody = attempt.ResponseBody!;
            var responseContent = IsJson(responseBody) ? new JsonText(responseBody) : (IRenderable)new Markup(responseBody);
            var responsePanel = CreatePanel(responseContent, "Response Body");
            var layout = new Layout().SplitColumns(new Layout(requestPanel), new Layout(responsePanel));
            AnsiConsole.Write(layout);
        }

        return 0;
    }

    private static Panel CreatePanel(IRenderable content, string header)
    {
        var aligned = Align.Center(content, VerticalAlignment.Middle);
        return new Panel(aligned).Header(header).RoundedBorder();
    }

    private static bool IsJson(string text)
    {
        try
        {
            JsonNode.Parse(text);
            return true;
        }
        catch (Exception) { return false; }
    }
}
