using System.Text.Json.Serialization;

namespace Falu.Oidc;

public class OidcDeviceAuthorizationResponse : OidcResponse
{
    /// <summary>The device verification code.</summary>
    /// <example>GmRhmhcxhwAzkoEqiMEg_DnyEysNkuNhszIySk9eS</example>
    [JsonPropertyName("device_code")]
    public required string DeviceCode { get; set; }

    /// <summary>The end-user verification code.</summary>
    /// <example>WDJB-MJHT</example>
    [JsonPropertyName("user_code")]
    public required string UserCode { get; set; }

    /// <summary>The end-user verification URI on the authorization server.</summary>
    /// <example>https://example.com/device</example>
    [JsonPropertyName("verification_uri")]
    public required string VerificationUri { get; set; }

    /// <summary>
    /// A verification URI that includes the <see cref="UserCode"/>
    /// (or other information with the same function as the <see cref="UserCode"/>),
    /// which is designed for non-textual transmission.
    /// </summary>
    /// <example>https://example.com/device?user_code=WDJB-MJHT</example>
    [JsonPropertyName("verification_uri_complete")]
    public string? VerificationUriComplete { get; set; }

    /// <summary>
    /// The lifetime in seconds of the <see cref="DeviceCode"/> and <see cref="UserCode"/>.
    /// </summary>
    /// <example>1800</example>
    [JsonPropertyName("expires_in")]
    public required int ExpiresIn { get; set; }

    /// <summary>
    /// The minimum amount of time in seconds that the client SHOULD wait between polling requests to the token endpoint.
    /// If no value is provided, clients MUST use 5 as the default.
    /// </summary>
    /// <example>5</example>
    [JsonPropertyName("interval")]
    public int Interval { get; set; } = 5;
}
