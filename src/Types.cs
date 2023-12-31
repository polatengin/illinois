using System.Text.Json;
using System.Text.Json.Serialization;

public record Root([property: JsonPropertyName("$schema")]string schema, string languageVersion, string contentVersion, Metadata metadata, JsonElement parameters, JsonElement variables, JsonElement resources, JsonElement outputs);

public record Metadata(string _EXPERIMENTAL_WARNING, IReadOnlyList<string> _EXPERIMENTAL_FEATURES_ENABLED);
