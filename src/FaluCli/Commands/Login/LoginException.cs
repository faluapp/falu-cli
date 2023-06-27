using Falu.Oidc;
using System.Runtime.Serialization;

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
    protected LoginException(SerializationInfo info, StreamingContext context) : base(info, context) { }

    public OidcResponse? Response { get; set; }
}
