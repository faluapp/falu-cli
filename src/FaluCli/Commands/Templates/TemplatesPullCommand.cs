namespace Falu.Commands.Templates;

public class TemplatesPullCommand : Command
{
    public TemplatesPullCommand() : base("pull", "Download templates from Falu servers to your local file system.")
    {
        this.AddArgument<string>(name: "output-directory",
                                 description: "The directory into which to put the pulled templates.");

        this.AddOption(new[] { "-o", "--overwrite", },
                       description: "Overwrite templates if they already exist.",
                       defaultValue: false);
    }
}
