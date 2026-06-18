using System.Text.Json;

using global::Typesense;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Integrations.Search.Typesense.Builders;
using Umbraco.Cms.Integrations.Search.Typesense.Migrations;
using Umbraco.Cms.Integrations.Search.Typesense.Models;
using Umbraco.Cms.Integrations.Search.Typesense.Services;

namespace Umbraco.Cms.Integrations.Search.Typesense.Controllers
{
    /// <summary>
    /// Management API endpoints for creating, building, searching and deleting Typesense indices
    /// (collections), backing the backoffice dashboard.
    /// </summary>
    public class IndexController : TypesenseControllerBase
    {
        private readonly ITypesenseIndexService _indexService;

        private readonly ITypesenseSearchService<SearchResult<Dictionary<string, object>>> _searchService;

        private readonly ITypesenseIndexDefinitionStorage<TypesenseIndex> _indexStorage;

        private readonly IUserService _userService;

        private readonly IPublishedUrlProvider _urlProvider;

        private readonly IContentService _contentService;

        private readonly IContentTypeService _contentTypeService;

        private readonly ITypesenseSearchPropertyIndexValueFactory _propertyIndexValueFactory;

        private readonly ITypesenseGeolocationProvider _geolocationProvider;

        private readonly IUmbracoContextFactory _umbracoContextFactory;

        private readonly ILogger<IndexController> _logger;

        private readonly IRecordBuilderFactory _recordBuilderFactory;

        public IndexController(
            ITypesenseIndexService indexService,
            ITypesenseSearchService<SearchResult<Dictionary<string, object>>> searchService,
            ITypesenseIndexDefinitionStorage<TypesenseIndex> indexStorage,
            IUserService userService,
            IPublishedUrlProvider urlProvider,
            IContentService contentService,
            IContentTypeService contentTypeService,
            ITypesenseSearchPropertyIndexValueFactory propertyIndexValueFactory,
            ITypesenseGeolocationProvider geolocationProvider,
            IUmbracoContextFactory umbracoContextFactory,
            ILogger<IndexController> logger,
            IRecordBuilderFactory recordBuilderFactory)
        {
            _indexService = indexService;
            _searchService = searchService;
            _indexStorage = indexStorage;
            _userService = userService;
            _urlProvider = urlProvider;
            _contentService = contentService;
            _contentTypeService = contentTypeService;
            _propertyIndexValueFactory = propertyIndexValueFactory;
            _geolocationProvider = geolocationProvider;
            _umbracoContextFactory = umbracoContextFactory;
            _logger = logger;
            _recordBuilderFactory = recordBuilderFactory;
        }

        [HttpGet("index")]
        [ProducesResponseType(typeof(IEnumerable<IndexConfiguration>), StatusCodes.Status200OK)]
        public IActionResult GetIndices()
        {
            var results = _indexStorage.Get().Select(p => new IndexConfiguration
            {
                Id = p.Id,
                Name = p.Name,
                ContentData = JsonSerializer.Deserialize<List<ContentData>>(p.SerializedData)
            });

            return Ok(results);
        }

        [HttpPost("index")]
        [ProducesResponseType(typeof(Result), StatusCodes.Status200OK)]
        public async Task<IActionResult> SaveIndex([FromBody] IndexConfiguration index)
        {
            try
            {
                _indexStorage.AddOrUpdate(new TypesenseIndex
                {
                    Id = index.Id,
                    Name = index.Name,
                    SerializedData = JsonSerializer.Serialize(index.ContentData),
                    Date = DateTime.Now
                });

                var result = await _indexService.IndexExists(index.Name)
                    ? Result.Ok()
                    : await _indexService.PushData(index.Name);

                return Ok(result);
            }
            catch (Exception ex)
            {
                return Ok(Result.Fail(ex.Message));
            }
        }

        [HttpPost("index/build")]
        [ProducesResponseType(typeof(Result), StatusCodes.Status200OK)]
        public async Task<IActionResult> BuildIndex([FromBody] IndexConfiguration indexConfiguration)
        {
            try
            {
                var index = _indexStorage.GetById(indexConfiguration.Id);

                var payload = new List<TypesenseRecord>();

                var indexContentData = JsonSerializer.Deserialize<List<ContentData>>(index.SerializedData);

                foreach (var contentDataItem in indexContentData)
                {
                    var contentType = _contentTypeService.Get(contentDataItem.ContentType.Alias);
                    if (contentType == null) continue;

                    var contentItems = _contentService.GetPagedOfType(contentType.Id, 0, int.MaxValue, out _, null);

                    _logger.LogInformation("Building Typesense index for {ContentType} with {Count} items",
                        contentDataItem.ContentType.Alias, contentItems.Count());

                    foreach (var contentItem in contentItems.Where(p => !p.Trashed))
                    {
                        var record = new ContentRecordBuilder(
                                _userService,
                                _urlProvider,
                                _propertyIndexValueFactory,
                                _recordBuilderFactory,
                                _umbracoContextFactory,
                                _geolocationProvider)
                            .BuildFromContent(contentItem, (p) => contentDataItem.Properties.Any(q => q.Alias == p.Alias))
                            .Build();

                        payload.Add(record);
                    }
                }

                var result = await _indexService.PushData(index.Name, payload);

                return Ok(result);
            }
            catch (Exception ex)
            {
                return Ok(Result.Fail(ex.Message));
            }
        }

        [HttpDelete("index/{id:int}")]
        [ProducesResponseType(typeof(Result), StatusCodes.Status200OK)]
        public async Task<IActionResult> DeleteIndex(int id)
        {
            try
            {
                var indexName = _indexStorage.GetById(id).Name;

                _indexStorage.Delete(id);

                await _indexService.DeleteIndex(indexName);

                return Ok(Result.Ok());
            }
            catch (Exception ex)
            {
                return Ok(Result.Fail(ex.Message));
            }
        }

        [HttpGet("search")]
        [ProducesResponseType(typeof(Response), StatusCodes.Status200OK)]
        public async Task<IActionResult> Search(int indexId, string query, int page = 1, int perPage = 10)
        {
            var index = _indexStorage.GetById(indexId);
            if (index == null) return NotFound();

            var queryBy = BuildQueryBy(index.SerializedData);

            var searchResults = await _searchService.SearchAsync(index.Name, query, queryBy, page, perPage);

            var response = new Response
            {
                ItemsCount = searchResults.Found,
                ItemsPerPage = perPage,
                PagesCount = perPage > 0 ? (int)Math.Ceiling((double)searchResults.Found / perPage) : 0,
                Hits = searchResults.Hits
                    .Select(p => p.Document.ToDictionary(x => x.Key, y => y.Value?.ToString() ?? string.Empty))
                    .ToList()
            };

            return Ok(response);
        }

        /// <summary>
        /// Typesense search requires a "query_by" list. Build it from "name" plus every selected
        /// property alias in the index definition.
        /// </summary>
        private static string BuildQueryBy(string serializedData)
        {
            var aliases = new List<string> { "name" };

            var contentData = JsonSerializer.Deserialize<List<ContentData>>(serializedData);
            if (contentData != null)
            {
                aliases.AddRange(contentData
                    .Where(c => c.Properties != null)
                    .SelectMany(c => c.Properties.Select(p => p.Alias)));
            }

            return string.Join(",", aliases.Distinct());
        }
    }
}
