using Falu.Oidc;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Falu.Config;

internal class ConfigValuesProvider : IConfigValuesProvider
{
    // Path example C:\Users\USERNAME\.config\falu\config.toml
    private static readonly string UserProfileFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
    private static readonly string FilePath = Path.Combine(UserProfileFolder, ".config", "falu", "config.json");
    private static readonly JsonSerializerOptions serializerOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault | JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true,
    };
    private static readonly FaluCliJsonSerializerContext serializerContext = new(new(serializerOptions));

    private ConfigValues? values;

    public async Task<ConfigValues> GetConfigValuesAsync(CancellationToken cancellationToken = default)
    {
        if (values is null)
        {
            if (File.Exists(FilePath))
            {
                var json = await File.ReadAllTextAsync(FilePath, cancellationToken);
                values = JsonSerializer.Deserialize(json, serializerContext.ConfigValues)!;
            }
            else
            {
                values = new ConfigValues();
            }
        }
        return values;
    }

    public async Task SaveConfigValuesAsync(CancellationToken cancellationToken = default)
    {
        // ensure the directory exists
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);

        values ??= await GetConfigValuesAsync(cancellationToken);
        var json = JsonSerializer.Serialize(values, serializerContext.ConfigValues);
        await File.WriteAllTextAsync(FilePath, json, cancellationToken);
    }

    public async Task SaveConfigValuesAsync(OidcTokenResponse response, CancellationToken cancellationToken = default)
    {
        values ??= await GetConfigValuesAsync(cancellationToken);
        values.Authentication = new AuthenticationTokenConfigData
        {
            AccessToken = response.AccessToken,
            RefreshToken = response.RefreshToken,
            AccessTokenExpiry = DateTimeOffset.UtcNow.AddSeconds(response.ExpiresIn).AddSeconds(-5),
        };

        await SaveConfigValuesAsync(cancellationToken);
    }

    public async Task ClearAuthenticationAsync(CancellationToken cancellationToken = default)
    {
        values ??= await GetConfigValuesAsync(cancellationToken);
        if (values.Authentication is not null)
        {
            values.Authentication = null;
            await SaveConfigValuesAsync(cancellationToken);
        }
    }

    public void ClearAll()
    {
        if (File.Exists(FilePath))
        {
            File.Delete(FilePath);
        }
    }
}
