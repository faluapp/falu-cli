namespace Falu.Commands.Templates;

public class TemplatesPushCommand : Command
{
    public TemplatesPushCommand() : base("push", "Pushes changed templates from the local file system to Falu servers.")
    {
        this.AddArgument<string>(name: "templates-directory",
                                 description: "The directory containing the templates.");

        this.AddOption(["-a", "--all"],
                       description: "Push all local templates up to Falu regardless of whether they changed.",
                       defaultValue: false);
    }
}
