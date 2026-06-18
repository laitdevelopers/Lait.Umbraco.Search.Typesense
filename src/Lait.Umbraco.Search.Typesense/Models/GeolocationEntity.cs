using System.Text.Json.Serialization;

namespace Umbraco.Cms.Integrations.Search.Typesense.Models
{
    /// <summary>
    /// Represents a geographic point. Serialized to a Typesense "geopoint" compatible structure.
    /// </summary>
    public class GeolocationEntity
    {
        [JsonPropertyName("lat")]
        public double Latitude { get; set; }

        [JsonPropertyName("lng")]
        public double Longitude { get; set; }
    }
}
