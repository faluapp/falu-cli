namespace Falu.Commands.Events;

internal class EventsListenCommand : Command
{
    public EventsListenCommand() : base("listen", "Listen to events")
    {
        this.AddOption<string[]>(new[] { "--event-type", "--type", "-t", },
                                 description: "The event to listen for. When not provided all events are listened to.");

        this.AddOption<Uri>(new[] { "--forward-to", "-f", },
                            description: "The URL that webhook events will be forwarded to.");

        this.AddOption(new[] { "--skip-validation", },
                       description: "Whether to skip certificate verification when forwarding to a HTTPS endpoint.",
                       defaultValue: false);

        this.AddOption<string>(new[] { "--webhook-secret", "--secret", },
                               description: "The webhook secret to use. You can generate a random one or use one from a webhook endpoint if you are testing signature validation.");
    }
}
