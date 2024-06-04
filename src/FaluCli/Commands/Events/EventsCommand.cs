namespace Falu.Commands.Events;

internal class EventsCommand : FaluCliCommand
{
    public EventsCommand() : base("events", "Work with events on Falu.")
    {
        Add(new EventsListenCommand());
        Add(new EventRetryCommand());
    }
}
