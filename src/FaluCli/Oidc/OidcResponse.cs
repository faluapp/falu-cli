using System.Text.Json.Serialization;

namespace Falu.Oidc;

public class OidcResponse
{
    [JsonPropertyName("error")]
    public string? Error { get; set; }

    [JsonPropertyName("error_description")]
    public string? ErrorDescription { get; set; }

    public bool IsError => Error is not null;
}
