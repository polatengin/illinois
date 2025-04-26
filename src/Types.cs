using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

public record Root([property: JsonPropertyName("$schema")]string schema, string languageVersion, string contentVersion, JsonElement metadata, JsonElement definitions, JsonArray functions, JsonElement parameters, JsonElement variables, JsonElement resources, JsonElement outputs);
