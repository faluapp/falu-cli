using Falu.Oidc;

namespace Falu.Commands.Login;

[Serializable]
public class LoginException : Exception
{
    public LoginException() { }
    public LoginException(string? message) : base(message) { }
    public LoginException(string? message, Exception? inner) : base(message, inner) { }
    public LoginException(OidcResponse response) : this(response.Error, inner: null)
    {
        Response = response;
    }

    public OidcResponse? Response { get; set; }
}
