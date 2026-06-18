namespace Umbraco.Cms.Integrations.Search.Typesense
{
    public class Constants
    {
        public const string SettingsPath = "Umbraco:Cms:Integrations:Search:Typesense:Settings";

        public const string TypesenseIndicesTableName = "typesenseIndices";

        public const string MigrationPlanName = "TypesenseIndices";

        /// <summary>
        /// Name of the custom Swagger/OpenAPI document for the Typesense Management API.
        /// </summary>
        public const string ApiName = "typesense";

        /// <summary>
        /// Route segment for the Typesense Management API controllers.
        /// </summary>
        public const string ApiRouteSegment = "typesense";
    }
}
