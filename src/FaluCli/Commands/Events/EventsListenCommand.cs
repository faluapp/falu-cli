namespace Falu.Commands.Events;

internal class EventsListenCommand : Command
{
    public EventsListenCommand() : base("listen", "Listen to events")
    {
        this.AddOption<string[]>(new[] { "--event-type", "--type", "-t", },
                                 description: "The event to listen for. When not provided all events are listened to.");
    }
}
