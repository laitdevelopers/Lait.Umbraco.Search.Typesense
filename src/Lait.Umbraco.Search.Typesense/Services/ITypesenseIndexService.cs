using Umbraco.Cms.Integrations.Search.Typesense.Models;

namespace Umbraco.Cms.Integrations.Search.Typesense.Services
{
    public interface ITypesenseIndexService
    {
        /// <summary>
        /// Ensures the collection exists with a schema matching <paramref name="contentData"/> and
        /// (optionally) imports a full set of documents into it. When <paramref name="payload"/> is
        /// null only the collection is created or extended.
        /// </summary>
        Task<Result> PushData(string name, List<TypesenseRecord> payload = null, IEnumerable<ContentData> contentData = null);

        /// <summary>
        /// Upserts a single document into the collection.
        /// </summary>
        Task<Result> UpdateData(string name, TypesenseRecord record, IEnumerable<ContentData> contentData = null);

        /// <summary>
        /// Deletes a single document from the collection by its id.
        /// </summary>
        Task<Result> DeleteData(string name, string id);

        /// <summary>
        /// Deletes the whole collection.
        /// </summary>
        Task<Result> DeleteIndex(string name);

        /// <summary>
        /// Returns whether a collection with the given name exists.
        /// </summary>
        Task<bool> IndexExists(string name);

        /// <summary>
        /// Returns the names of the fields that Typesense will accept in a "query_by" list, i.e.
        /// the string and string[] fields declared on the collection.
        /// </summary>
        Task<IReadOnlyCollection<string>> GetQueryableFields(string name);
    }
}
