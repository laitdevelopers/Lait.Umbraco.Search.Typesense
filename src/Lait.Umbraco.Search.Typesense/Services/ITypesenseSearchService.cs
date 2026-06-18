namespace Umbraco.Cms.Integrations.Search.Typesense.Services
{
    public interface ITypesenseSearchService<T>
    {
        /// <summary>
        /// Runs a query against the named collection.
        /// </summary>
        /// <param name="collectionName">The Typesense collection to search.</param>
        /// <param name="query">The query text (use "*" to match everything).</param>
        /// <param name="queryBy">Comma-separated list of fields to query against (required by Typesense).</param>
        /// <param name="page">1-based page number.</param>
        /// <param name="perPage">Number of results per page.</param>
        Task<T> SearchAsync(string collectionName, string query, string queryBy, int page = 1, int perPage = 10);
    }
}
