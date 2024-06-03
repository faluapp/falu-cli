using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Falu.Config;

internal class ConfigValuesLoader
{
    // Path example C:\Users\USERNAME\.config\falu\config.toml
    private static readonly string UserProfileFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
    private static readonly string FilePath = Path.Combine(UserProfileFolder, ".config", "falu", "config.json");
    private static readonly JsonSerializerOptions serializerOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault | JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    private ConfigValues? values;
    private string? hash;

    public virtual async Task<ConfigValues> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (values is not null) return values;

        if (File.Exists(FilePath))
        {
            var json = await File.ReadAllTextAsync(FilePath, cancellationToken);
            values = new ConfigValues(JsonNode.Parse(json)!.AsObject());
            hash = values.Hash();
        }
        else
        {
            values = new ConfigValues([]);
        }

        return values;
    }

    public virtual async Task SaveAsync(ConfigValues values, CancellationToken cancellationToken = default)
    {
        if (string.Equals(hash, values.Hash(), StringComparison.Ordinal)) return;

        // the contents have changed, save them
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!); // ensure the directory exists
        await File.WriteAllTextAsync(FilePath, values.Json(serializerOptions), cancellationToken);
    }
}
