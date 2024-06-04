namespace Falu.Commands.Templates;

internal class TemplatesCommand : FaluCliCommand
{
    public TemplatesCommand() : base("templates", "Manage message templates.")
    {
        Add(new TemplatesPullCommand());
        Add(new TemplatesPushCommand());
    }
}
