using System.Text.Json;
using System.Text.Json.Serialization;

namespace Slic3rPostProcessingUploader.Services.Parsers
{
    internal class CuraSettingDto
    {
        public required string Slicer { get; set; }
        public required string CuraVersion { get; set; }

        /// <summary>
        /// Set by <see cref="PrintMetadata.Apply"/> after parsing; null until then.
        /// </summary>
        public string? PluginVersion { get; set; }

        public required CuraSettings settings { get; set; }

        /// <summary>
        /// The JSON body posted to the API. Property names are camel-cased by <see cref="JsonContext"/>.
        /// </summary>
        public string ToJSON()
        {
            return JsonSerializer.Serialize(this, JsonContext.Default.CuraSettingDto);
        }
    }

    public class CuraSettings
    {
        public string note { get; set; } = "";

        /// <summary>
        /// Set by <see cref="PrintMetadata.Apply"/> after parsing; null until then.
        /// </summary>
        public string? print_name { get; set; }
        public int estimated_print_time_seconds { get; set; }
        public int? material_used_mg { get; set; }

        /// <summary>
        /// Base64 encoded image
        /// </summary>
        public string? Snapshot { get; set; }
        public string? file_name { get; set; }

        public List<PrintFilamentSummaryDto>? filamentUsage { get; set; }
    }

    [JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Serialization, PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, WriteIndented = true)]
    [JsonSerializable(typeof(CuraSettingDto))]
    partial class JsonContext : JsonSerializerContext
    {
    }
}
