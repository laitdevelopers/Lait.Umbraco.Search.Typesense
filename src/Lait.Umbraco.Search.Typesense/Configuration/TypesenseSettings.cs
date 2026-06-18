namespace Umbraco.Cms.Integrations.Search.Typesense.Configuration
{
    public class TypesenseSettings
    {
        /// <summary>
        /// Hostname of the Typesense node (e.g. "localhost" or "xxx.a1.typesense.net").
        /// </summary>
        public string Host { get; set; } = "localhost";

        /// <summary>
        /// Port of the Typesense node. Typically "8108" for self-hosted, "443" for Typesense Cloud.
        /// May be left empty, in which case the protocol's standard port (443 for https, 80 for http) is used.
        /// </summary>
        public string Port { get; set; } = "8108";

        /// <summary>
        /// Protocol used to connect to the node ("http" or "https").
        /// </summary>
        public string Protocol { get; set; } = "http";

        /// <summary>
        /// Admin API key used for managing collections and documents (create / import / delete).
        /// </summary>
        public string ApiKey { get; set; } = string.Empty;

        /// <summary>
        /// Optional search-only API key. Reserved for issuing scoped search keys to the frontend.
        /// When empty, the admin <see cref="ApiKey"/> is used for search requests.
        /// </summary>
        public string SearchApiKey { get; set; } = string.Empty;
    }
}
