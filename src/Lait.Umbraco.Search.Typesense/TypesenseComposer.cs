using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Typesense;
using Typesense.Setup;

using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Integrations.Search.Typesense.Builders;
using Umbraco.Cms.Integrations.Search.Typesense.Configuration;
using Umbraco.Cms.Integrations.Search.Typesense.Extensions;
using Umbraco.Cms.Integrations.Search.Typesense.Handlers;
using Umbraco.Cms.Integrations.Search.Typesense.Migrations;
using Umbraco.Cms.Integrations.Search.Typesense.Services;

namespace Umbraco.Cms.Integrations.Search.Typesense
{
    public class TypesenseComposer : IComposer
    {
        public void Compose(IUmbracoBuilder builder)
        {
            // Database migration for the index definitions table.
            builder.AddNotificationHandler<UmbracoApplicationStartingNotification, RunTypesenseIndicesMigration>();

            // Keep Typesense in sync when content is published / changed.
            builder.AddNotificationAsyncHandler<ContentCacheRefresherNotification, TypesenseContentCacheRefresherHandler>();
            builder.AddNotificationAsyncHandler<ContentPublishedNotification, TypesenseContentPublishedHandler>();

            builder.Services.AddOptions<TypesenseSettings>()
                .Bind(builder.Config.GetSection(Constants.SettingsPath));

            // Register the Typesense client from configuration (works for self-hosted and Typesense Cloud).
            var settings = builder.Config.GetSection(Constants.SettingsPath).Get<TypesenseSettings>() ?? new TypesenseSettings();
            builder.Services.AddTypesenseClient(config =>
            {
                config.ApiKey = settings.ApiKey;
                config.SearchApiKey = string.IsNullOrWhiteSpace(settings.SearchApiKey) ? settings.ApiKey : settings.SearchApiKey;
                config.Nodes = new List<Node>
                {
                    new Node(settings.Host, ResolvePort(settings.Port, settings.Protocol), settings.Protocol)
                };
            });

            builder.Services.AddSingleton<ITypesenseIndexService, TypesenseIndexService>();

            builder.Services.AddSingleton<ITypesenseSearchService<SearchResult<Dictionary<string, object>>>, TypesenseSearchService>();

            builder.Services.AddScoped<ITypesenseIndexDefinitionStorage<TypesenseIndex>, TypesenseIndexDefinitionStorage>();

            builder.Services.AddScoped<IRecordBuilderFactory, RecordBuilderFactory>();

            builder.Services.AddScoped<ITypesenseSearchPropertyIndexValueFactory, TypesenseSearchPropertyIndexValueFactory>();

            builder.Services.AddSingleton<ITypesenseGeolocationProvider, TypesenseNullGeolocationProvider>();

            builder.AddTypesenseConverters();

            // Register the custom Swagger / OpenAPI document for the Typesense Management API.
            builder.Services.ConfigureOptions<ConfigureTypesenseSwaggerGenOptions>();
        }

        /// <summary>
        /// Allows the configured port to be empty: falls back to the protocol's standard port
        /// (443 for https, otherwise 80) so the node URL stays well-formed. Useful for Typesense
        /// Cloud / reverse-proxy setups served on standard ports.
        /// </summary>
        private static string ResolvePort(string port, string protocol)
        {
            if (!string.IsNullOrWhiteSpace(port)) return port;

            return string.Equals(protocol, "https", StringComparison.OrdinalIgnoreCase) ? "443" : "80";
        }
    }
}
