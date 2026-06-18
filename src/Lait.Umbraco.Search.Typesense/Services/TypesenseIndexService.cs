using global::Typesense;

using Umbraco.Cms.Integrations.Search.Typesense.Models;

namespace Umbraco.Cms.Integrations.Search.Typesense.Services
{
    public class TypesenseIndexService : ITypesenseIndexService
    {
        private readonly ITypesenseClient _client;

        public TypesenseIndexService(ITypesenseClient client)
        {
            _client = client;
        }

        public async Task<Result> PushData(string name, List<TypesenseRecord> payload = null)
        {
            try
            {
                await EnsureCollection(name);

                if (payload != null && payload.Count > 0)
                {
                    var documents = payload.Select(p => p.ToDocument()).ToList();

                    await _client.ImportDocuments(name, documents, batchSize: 100, importType: ImportType.Upsert);
                }

                return Result.Ok();
            }
            catch (Exception ex)
            {
                return Result.Fail(ex.Message);
            }
        }

        public async Task<Result> UpdateData(string name, TypesenseRecord record)
        {
            try
            {
                await EnsureCollection(name);

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

        public async Task<bool> IndexExists(string name)
        {
            var collections = await _client.RetrieveCollections();

            return collections.Any(p => p.Name == name);
        }

        /// <summary>
        /// Creates the collection with an auto-schema wildcard field if it does not already exist.
        /// This mirrors the schemaless behaviour of the Algolia integration: any flattened document
        /// key is indexed automatically by Typesense.
        /// </summary>
        private async Task EnsureCollection(string name)
        {
            if (await IndexExists(name)) return;

            var schema = new Schema(
                name,
                new List<Field>
                {
                    new Field(".*", FieldType.Auto)
                })
            {
                EnableNestedFields = true
            };

            await _client.CreateCollection(schema);
        }
    }
}
