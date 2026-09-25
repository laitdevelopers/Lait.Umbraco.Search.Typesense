using global::Typesense;

using Umbraco.Cms.Integrations.Search.Typesense.Models;

namespace Umbraco.Cms.Integrations.Search.Typesense.Services
{
    public class TypesenseIndexService : ITypesenseIndexService
    {
        private const int MaxReportedErrors = 5;

        private readonly ITypesenseClient _client;

        private readonly ITypesenseSchemaBuilder _schemaBuilder;

        public TypesenseIndexService(ITypesenseClient client, ITypesenseSchemaBuilder schemaBuilder)
        {
            _client = client;
            _schemaBuilder = schemaBuilder;
        }

        public async Task<Result> PushData(string name, List<TypesenseRecord> payload = null, IEnumerable<ContentData> contentData = null)
        {
            try
            {
                var ensured = await EnsureCollection(name, contentData);
                if (ensured.Failure) return ensured;

                if (payload == null || payload.Count == 0) return Result.Ok();

                var documents = payload.Select(p => p.ToDocument()).ToList();

                var responses = await _client.ImportDocuments(name, documents, batchSize: 100, importType: ImportType.Upsert);

                return Summarize(responses, documents.Count);
            }
            catch (Exception ex)
            {
                return Result.Fail(ex.Message);
            }
        }

        public async Task<Result> UpdateData(string name, TypesenseRecord record, IEnumerable<ContentData> contentData = null)
        {
            try
            {
                var ensured = await EnsureCollection(name, contentData);
                if (ensured.Failure) return ensured;

                await _client.UpsertDocument(name, record.ToDocument());

                return Result.Ok();
            }
            catch (Exception ex)
            {
                return Result.Fail(ex.Message);
            }
        }

        public async Task<Result> DeleteData(string name, string id)
        {
            try
            {
                await _client.DeleteDocument<Dictionary<string, object>>(name, id);

                return Result.Ok();
            }
            catch (Exception ex)
            {
                return Result.Fail(ex.Message);
            }
        }

        public async Task<Result> DeleteIndex(string name)
        {
            try
            {
                await _client.DeleteCollection(name);

                return Result.Ok();
            }
            catch (Exception ex)
            {
                return Result.Fail(ex.Message);
            }
        }

        public async Task<bool> IndexExists(string name) => await RetrieveCollection(name) != null;

        public async Task<IReadOnlyCollection<string>> GetQueryableFields(string name)
        {
            var collection = await RetrieveCollection(name);
            if (collection?.Fields == null) return Array.Empty<string>();

            return collection.Fields
                .Where(p => p.Type == FieldType.String || p.Type == FieldType.StringArray)
                .Select(p => p.Name)
                .ToList();
        }

        /// <summary>
        /// Reports how the import went. Typesense answers an import per document, so a batch can
        /// come back with individual rejections while the request itself succeeded; those used to be
        /// discarded, which is why the dashboard reported a successful build for an index that had
        /// taken no documents at all.
        /// </summary>
        private static Result Summarize(IEnumerable<ImportResponse> responses, int documentCount)
        {
            var failures = responses?.Where(p => !p.Success).ToList() ?? new List<ImportResponse>();
            if (failures.Count == 0) return Result.Ok();

            var errors = failures
                .Select(p => p.Error)
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .Distinct()
                .Take(MaxReportedErrors);

            return Result.Fail(
                $"Typesense rejected {failures.Count} of {documentCount} documents: {string.Join(" | ", errors)}");
        }

        private async Task<CollectionResponse> RetrieveCollection(string name)
        {
            var collections = await _client.RetrieveCollections();

            return collections.FirstOrDefault(p => p.Name == name);
        }

        /// <summary>
        /// Creates the collection with an explicit schema derived from the index definition, or
        /// brings an existing collection up to date by adding the fields it is missing.
        /// </summary>
        /// <remarks>
        /// Fields are never dropped or retyped: Typesense refuses a type change that the stored
        /// documents cannot be coerced into, so a collection left over from an earlier definition is
        /// reported instead of silently mangled.
        /// </remarks>
        private async Task<Result> EnsureCollection(string name, IEnumerable<ContentData> contentData)
        {
            var fields = _schemaBuilder.BuildFields(contentData);

            var existing = await RetrieveCollection(name);
            if (existing == null)
            {
                await _client.CreateCollection(new Schema(name, fields.ToList()) { EnableNestedFields = true });

                return Result.Ok();
            }

            var existingFields = existing.Fields?
                .GroupBy(p => p.Name, StringComparer.Ordinal)
                .ToDictionary(p => p.Key, p => p.First().Type, StringComparer.Ordinal)
                ?? new Dictionary<string, FieldType>(StringComparer.Ordinal);

            var conflicts = fields
                .Where(p => existingFields.TryGetValue(p.Name, out var type) && type != p.Type)
                .Select(p => $"'{p.Name}' is {existingFields[p.Name]}, expected {p.Type}")
                .ToList();

            if (conflicts.Count > 0)
            {
                return Result.Fail(
                    $"Collection '{name}' was built with a different schema ({string.Join("; ", conflicts)}). "
                    + "Delete the index and create it again to rebuild it with the current definition.");
            }

            var missing = fields
                .Where(p => p.Name != ".*" && !existingFields.ContainsKey(p.Name))
                .Select(p => new UpdateSchemaField(p.Name, p.Type, p.Facet, p.Optional, p.Index, p.Sort))
                .ToList();

            if (missing.Count > 0)
            {
                await _client.UpdateCollection(name, new UpdateSchema(missing));
            }

            return Result.Ok();
        }
    }
}
