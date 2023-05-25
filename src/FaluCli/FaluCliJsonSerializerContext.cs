using System.Text.Json;
using System.Text.Json.Serialization;

namespace Falu;

[JsonSerializable(typeof(List<Client.ExtractedStatementRecord>))]
internal partial class FaluCliJsonSerializerContext : JsonSerializerContext
{
    private static JsonSerializerOptions DefaultSerializerOptions { get; } = new(JsonSerializerDefaults.Web)
    {
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip,

        // Ignore default values to reduce the data sent after serialization
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,

        // Do not indent content to reduce data usage
        WriteIndented = false,

        // TODO; change to snake case in .NET 8
        //// Use SnakeCase because it is what the server provides
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DictionaryKeyPolicy = null,
    };

    static FaluCliJsonSerializerContext() => s_defaultContext = new FaluCliJsonSerializerContext(new JsonSerializerOptions(DefaultSerializerOptions));
}
