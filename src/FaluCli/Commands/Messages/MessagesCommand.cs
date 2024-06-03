namespace Falu.Commands.Messages;

internal class MessagesCommand : CliCommand
{
    public MessagesCommand() : base("messages", "Work with messages.")
    {
        Add(new MessagesSendRawCommand());
        Add(new MessagesSendTemplatedCommand());
    }
}
