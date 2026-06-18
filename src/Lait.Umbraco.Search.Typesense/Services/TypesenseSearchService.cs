using global::Typesense;

namespace Umbraco.Cms.Integrations.Search.Typesense.Services
{
    public class TypesenseSearchService : ITypesenseSearchService<SearchResult<Dictionary<string, object>>>
    {
        private readonly ITypesenseClient _client;

        public TypesenseSearchService(ITypesenseClient client)
        {
            _client = client;
        }

        public async Task<SearchResult<Dictionary<string, object>>> SearchAsync(
            string collectionName,
            string query,
            string queryBy,
            int page = 1,
            int perPage = 10)
        {
            var searchParameters = new SearchParameters(
                string.IsNullOrWhiteSpace(query) ? "*" : query,
                string.IsNullOrWhiteSpace(queryBy) ? "name" : queryBy)
            {
                Page = page,
                PerPage = perPage
            };

            return await _client.Search<Dictionary<string, object>>(collectionName, searchParameters);
        }
    }
}
