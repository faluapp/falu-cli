namespace Falu.Commands.Events;

internal class EventRetryCommand : Command
{
    public EventRetryCommand() : base("retry", "Retry delivery of an event to a webhook endpoint.")
    {
        this.AddArgument(name: "event",
                         description: "Unique identifier of the event. Example: evt_610010be9228355f14ce6e08",
                         format: Constants.EventIdFormat);

        this.AddOption(aliases: new[] { "--webhook-endpoint" },
                       description: "Unique identifier of the webhook endpoint. Example: we_610010be9228355f14ce6e08",
                       format: Constants.WebhookEndpointIdFormat,
                       configure: o => o.IsRequired = true);
    }
}
