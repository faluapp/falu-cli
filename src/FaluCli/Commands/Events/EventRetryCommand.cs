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
    private readonly CliArgument<string> eventArg;
    private readonly CliOption<string> webhookEndpointOption;

    public EventRetryCommand() : base("retry", "Retry delivery of an event to a webhook endpoint.")
    {
        eventArg = new CliArgument<string>(name: "event") { Description = "Unique identifier of the event. Example: evt_610010be9228355f14ce6e08" };
        eventArg.MatchesFormat(Constants.EventIdFormat);
        Add(eventArg);

        webhookEndpointOption = new CliOption<string>(name: "--webhook-endpoint")
        {
            Description = "Unique identifier of the webhook endpoint. Example: we_610010be9228355f14ce6e08",
            Required = true,
        };
        webhookEndpointOption.MatchesFormat(Constants.WebhookEndpointIdFormat, nulls: true);
        Add(webhookEndpointOption);
    }

    public override async Task<int> ExecuteAsync(CliCommandExecutionContext context, CancellationToken cancellationToken)
    {
        var eventId = context.ParseResult.GetValue(eventArg)!;
        var webhookEndpointId = context.ParseResult.GetValue(webhookEndpointOption);

        var options = new EventDeliveryRetryOptions { WebhookEndpoint = webhookEndpointId, };
        var response = await context.Client.Events.RetryAsync(eventId, options, cancellationToken: cancellationToken);
        response.EnsureSuccess();

        var attempt = response.Resource!;

        var time = TimeSpan.FromMilliseconds(attempt.ResponseTime);
        var data = new Dictionary<string, object> { ["Attempted"] = $"{attempt.Attempted:F}", ["Url"] = attempt.Url!, ["Response Time"] = $"{time.TotalSeconds:n3} seconds", };

        var verbose = IsVerboseEnabled(context.ParseResult);
        if (!attempt.Successful && verbose)
        {
            var statusCode = Enum.Parse<HttpStatusCode>(attempt.HttpStatus.ToString());
            data["Http Status"] = $"{statusCode} ({attempt.HttpStatus})";
        }

        var sb = new StringBuilder();
        sb.AppendLine(attempt.Successful ? SpectreFormatter.ColouredGreen("Retry succeeded.") : SpectreFormatter.ColouredRed("Retry failed!"));
        sb.AppendLine(data.MakePaddedString());
        AnsiConsole.MarkupLine(sb.ToString());

        if (verbose)
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
