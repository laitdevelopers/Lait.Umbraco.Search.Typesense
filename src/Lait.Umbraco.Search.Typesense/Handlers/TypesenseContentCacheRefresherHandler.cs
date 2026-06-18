using System.Text.Json;

using Microsoft.Extensions.Logging;

using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Services.Changes;
using Umbraco.Cms.Core.Sync;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Integrations.Search.Typesense.Builders;
using Umbraco.Cms.Integrations.Search.Typesense.Migrations;
using Umbraco.Cms.Integrations.Search.Typesense.Models;
using Umbraco.Cms.Integrations.Search.Typesense.Services;

namespace Umbraco.Cms.Integrations.Search.Typesense.Handlers
{
    public class TypesenseContentCacheRefresherHandler : INotificationAsyncHandler<ContentCacheRefresherNotification>
    {
        private readonly IServerRoleAccessor _serverRoleAccessor;

        private readonly IContentService _contentService;

        private readonly ILogger<TypesenseContentCacheRefresherHandler> _logger;

        private readonly ITypesenseIndexDefinitionStorage<TypesenseIndex> _indexStorage;

        private readonly ITypesenseIndexService _indexService;

        private readonly IUserService _userService;

        private readonly IPublishedUrlProvider _urlProvider;

        private readonly ITypesenseSearchPropertyIndexValueFactory _propertyIndexValueFactory;

        private readonly ITypesenseGeolocationProvider _geolocationProvider;

        private readonly IRecordBuilderFactory _recordBuilderFactory;

        private readonly IUmbracoContextFactory _umbracoContextFactory;

        public TypesenseContentCacheRefresherHandler(
            IServerRoleAccessor serverRoleAccessor,
            ILogger<TypesenseContentCacheRefresherHandler> logger,
            IContentService contentService,
            ITypesenseIndexDefinitionStorage<TypesenseIndex> indexStorage,
            ITypesenseIndexService indexService,
            IUserService userService,
            IPublishedUrlProvider urlProvider,
            ITypesenseSearchPropertyIndexValueFactory propertyIndexValueFactory,
            ITypesenseGeolocationProvider geolocationProvider,
            IRecordBuilderFactory recordBuilderFactory,
            IUmbracoContextFactory umbracoContextFactory)
        {
            _serverRoleAccessor = serverRoleAccessor;
            _contentService = contentService;
            _logger = logger;
            _indexStorage = indexStorage;
            _indexService = indexService;
            _userService = userService;
            _urlProvider = urlProvider;
            _propertyIndexValueFactory = propertyIndexValueFactory;
            _geolocationProvider = geolocationProvider;
            _recordBuilderFactory = recordBuilderFactory;
            _umbracoContextFactory = umbracoContextFactory;
        }

        public async Task HandleAsync(ContentCacheRefresherNotification notification, CancellationToken cancellationToken)
        {
            if (notification.MessageObject is not ContentCacheRefresher.JsonPayload[] payloads)
            {
                return;
            }

            switch (_serverRoleAccessor.CurrentServerRole)
            {
                case ServerRole.Subscriber:
                    _logger.LogDebug("Typesense indexing task will not run on subscriber servers.");
                    return;
                case ServerRole.Unknown:
                    _logger.LogDebug("Typesense indexing task will not run on servers with unknown role.");
                    return;
                case ServerRole.Single:
                case ServerRole.SchedulingPublisher:
                default:
                    break;
            }

            var refreshedContent = _contentService
                .GetByIds(
                    payloads
                        .Where(p => p.ChangeTypes == TreeChangeTypes.RefreshNode || p.ChangeTypes == TreeChangeTypes.RefreshBranch)
                        .Select(p => p.Id));

            await RebuildIndex(refreshedContent);
        }

        protected async Task RebuildIndex(IEnumerable<IContent> entities)
        {
            try
            {
                var indices = _indexStorage.Get();

                foreach (var entity in entities)
                {
                    foreach (var index in indices)
                    {
                        var indexConfiguration = JsonSerializer.Deserialize<List<ContentData>>(index.SerializedData)
                            .FirstOrDefault(p => p.ContentType.Alias == entity.ContentType.Alias);
                        if (indexConfiguration == null || indexConfiguration.ContentType.Alias != entity.ContentType.Alias) continue;

                        var record = new ContentRecordBuilder(
                                _userService,
                                _urlProvider,
                                _propertyIndexValueFactory,
                                _recordBuilderFactory,
                                _umbracoContextFactory,
                                _geolocationProvider)
                           .BuildFromContent(entity, (p) => indexConfiguration.Properties.Any(q => q.Alias == p.Alias))
                           .Build();

                        var result = entity.Trashed || !entity.Published
                            ? await _indexService.DeleteData(index.Name, entity.Key.ToString())
                            : await _indexService.UpdateData(index.Name, record);

                        if (result.Failure)
                            _logger.LogError("Failed to update data for Typesense index: {Error}", result.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to update data for Typesense index: {Message}", ex.Message);
            }
        }
    }
}
