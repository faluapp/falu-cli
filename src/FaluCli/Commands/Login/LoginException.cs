using IdentityModel.Client;
using System.Runtime.Serialization;

namespace Falu.Commands.Login;

[Serializable]
public class LoginException : Exception
{
    public LoginException() { }
    public LoginException(string? message) : base(message) { }
    public LoginException(string? message, Exception? inner) : base(message, inner) { }
    public LoginException(ProtocolResponse response) : this(response.Error, response.Exception)
    {
        Response = response;
    }
    protected LoginException(SerializationInfo info, StreamingContext context) : base(info, context) { }

    public ProtocolResponse? Response { get; }
}
