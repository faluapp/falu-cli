using Falu.Client;
using Falu.Client.Events;
using Spectre.Console;
using Spectre.Console.Json;
using Spectre.Console.Rendering;
using System.Net;
using System.Text;
using System.Text.Json.Nodes;

namespace Falu.Commands.Events;

internal class EventRetryCommandHandler(FaluCliClient client) : ICommandHandler
{
    int ICommandHandler.Invoke(InvocationContext context) => throw new NotImplementedException();

    public async Task<int> InvokeAsync(InvocationContext context)
    {
        var cancellationToken = context.GetCancellationToken();
        var eventId = context.ParseResult.ValueForArgument<string>("event");
        var webhookEndpointId = context.ParseResult.ValueForOption<string>("--webhook-endpoint");

        var model = new EventDeliveryRetry { WebhookEndpoint = webhookEndpointId, };
        var response = await client.Events.RetryAsync(eventId!, model, cancellationToken: cancellationToken);
        response.EnsureSuccess();

        var attempt = response.Resource!;

        var time = TimeSpan.FromMilliseconds(attempt.ResponseTime);
        var data = new Dictionary<string, object>
        {
            ["Attempted"] = $"{attempt.Attempted:F}",
            ["Url"] = attempt.Url!,
            ["Response Time"] = $"{time.TotalSeconds:n3} seconds",
        };

        if (!attempt.Successful && context.IsVerboseEnabled())
        {
            var statusCode = Enum.Parse<HttpStatusCode>(attempt.HttpStatus.ToString());
            data["Http Status"] = $"{statusCode} ({attempt.HttpStatus})";
        }

        var sb = new StringBuilder();
        sb.AppendLine(attempt.Successful ? SpectreFormatter.ColouredGreen("Retry succeeded.") : SpectreFormatter.ColouredRed("Retry failed!"));
        sb.AppendLine(data.MakePaddedString());
        AnsiConsole.MarkupLine(sb.ToString());

        if (context.IsVerboseEnabled())
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
