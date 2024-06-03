namespace Falu.Commands.Templates;

internal class TemplatesCommand : CliCommand
{
    public TemplatesCommand() : base("templates", "Manage message templates.")
    {
        Add(new TemplatesPullCommand());
        Add(new TemplatesPushCommand());
    }
}
