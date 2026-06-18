using Umbraco.Cms.Integrations.Search.Typesense.Models;

namespace Umbraco.Cms.Integrations.Search.Typesense.Services
{
    public interface ITypesenseIndexService
    {
        /// <summary>
        /// Ensures the collection exists and (optionally) imports a full set of documents into it.
        /// When <paramref name="payload"/> is null only the empty collection is created.
        /// </summary>
        Task<Result> PushData(string name, List<TypesenseRecord> payload = null);

        /// <summary>
        /// Upserts a single document into the collection.
        /// </summary>
        Task<Result> UpdateData(string name, TypesenseRecord record);

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
    }
}
