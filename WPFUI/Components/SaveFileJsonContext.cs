using System.Text.Json.Serialization;

namespace WPFUI.Components;

[JsonSerializable(typeof(SaveFile))]
[JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Default)]
public partial class SaveFileJsonContext : JsonSerializerContext
{
}