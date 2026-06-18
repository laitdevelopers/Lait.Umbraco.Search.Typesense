using System.Text.Json.Serialization;

namespace Umbraco.Cms.Integrations.Search.Typesense.Models
{
    public class MediaItem
    {
        [JsonPropertyName("key")]
        public string Key { get; set; }

        [JsonPropertyName("mediaKey")]
        public string MediaKey { get; set; }

        public string Name { get; set; }

        public string Url { get; set; }
    }
}
