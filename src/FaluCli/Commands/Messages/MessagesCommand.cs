namespace Falu.Commands.Messages;

internal class MessagesCommand : FaluCliCommand
{
    public MessagesCommand() : base("messages", "Work with messages.")
    {
        Add(new MessagesSendRawCommand());
        Add(new MessagesSendTemplatedCommand());
    }
}
