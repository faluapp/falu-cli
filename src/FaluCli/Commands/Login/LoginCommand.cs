namespace Falu.Commands.Login;

internal class LoginCommand : Command
{
    public LoginCommand() : base("login", "Login to your Falu account to setup the CLI")
    {
        this.AddOption(["--no-browser"],
                       description: "Set true to not open the browser automatically for authentication.",
                       defaultValue: false);

        //this.AddOption(["-i", "--interactive"],
        //               description: "Run interactive configuration mode if you cannot open a browser.",
        //               defaultValue: false);
    }
}
