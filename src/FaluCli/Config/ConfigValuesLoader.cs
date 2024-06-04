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
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping, // ensure we do not escape things like '+' when writing
    };

    private string? hash;
    private ConfigValues? values;

    public virtual async Task<ConfigValues> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (values is not null) return values;

        // load the file and parse it
        var inner = new JsonObject();
        if (File.Exists(FilePath))
        {
            await using var stream = File.OpenRead(FilePath);
            inner = (await JsonNode.ParseAsync(stream, cancellationToken: cancellationToken))!.AsObject();
        }

        // compute the hash and create the values
        hash = Hash(inner.ToJsonString(serializerOptions));
        values = new ConfigValues(inner);

        return values;
    }

    public virtual async Task SaveAsync(ConfigValues values, CancellationToken cancellationToken = default)
    {
        var json = values.Json(serializerOptions);
        if (string.Equals(hash, Hash(json), StringComparison.Ordinal)) return;

        // the contents have changed, save them
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!); // ensure the directory exists
        await File.WriteAllTextAsync(FilePath, json, cancellationToken);
    }

    private static string Hash(string json) => Convert.ToBase64String(System.Security.Cryptography.MD5.HashData(System.Text.Encoding.UTF8.GetBytes(json)));
}
