using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Falu.Config;

internal class ConfigValuesLoader
{
    // Path example C:\Users\USERNAME\.config\falu\config.json
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

        var inner = new JsonObject();
        if (File.Exists(FilePath))
        {
            await using var stream = File.OpenRead(FilePath);
            inner = (await JsonNode.ParseAsync(stream, cancellationToken: cancellationToken))!.AsObject();
        }

        values = new ConfigValues(inner);
        hash = values.Hash();

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
