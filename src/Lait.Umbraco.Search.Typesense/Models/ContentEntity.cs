using System.Text.Json.Serialization;

namespace Umbraco.Cms.Integrations.Search.Typesense.Models
{
    public class ContentEntity
    {
        [JsonPropertyName("alias")]
        public string Alias { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("icon")]
        public string Icon { get; set; }
    }
}
