using global::Typesense;

using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Integrations.Search.Typesense.Converters;
using Umbraco.Cms.Integrations.Search.Typesense.Models;
using Umbraco.Cms.Integrations.Search.Typesense.Providers;
using Umbraco.Extensions;

namespace Umbraco.Cms.Integrations.Search.Typesense.Services
{
    /// <summary>
    /// Builds an explicit collection schema from an index definition.
    /// </summary>
    /// <remarks>
    /// Collections used to be created with nothing but a ".*" auto field, which left Typesense to
    /// infer each field's type from the first document that happened to carry it. That is fragile:
    /// once a field is inferred as, say, <c>int64</c> (because the first product's decimal value was
    /// a whole number) every later document with a fractional value is rejected, and Typesense
    /// rejects the whole document and stops registering any field that sorts after the offending one.
    /// Declaring the types up front removes the guesswork, and also means "query_by" can reference a
    /// property that no document has filled in yet.
    /// </remarks>
    public class TypesenseSchemaBuilder : ITypesenseSchemaBuilder
    {
        private readonly IContentTypeService _contentTypeService;

        private readonly ILanguageService _languageService;

        private readonly ConverterCollection _converterCollection;

        public TypesenseSchemaBuilder(
            IContentTypeService contentTypeService,
            ILanguageService languageService,
            ConverterCollection converterCollection)
        {
            _contentTypeService = contentTypeService;
            _languageService = languageService;
            _converterCollection = converterCollection;
        }

        public IReadOnlyList<Field> BuildFields(IEnumerable<ContentData> contentData)
        {
            var fields = new List<Field>();
            var seen = new HashSet<string>(StringComparer.Ordinal);

            void Add(string name, FieldType type, bool sort = false, bool facet = false)
            {
                if (string.IsNullOrWhiteSpace(name) || !seen.Add(name)) return;

                fields.Add(new Field(name, type, facet: facet, optional: true, index: true, sort: sort));
            }

            // Built-in fields, matching TypesenseRecord.ToDocument().
            Add("contentId", FieldType.Int64, sort: true);
            Add("name", FieldType.String);
            Add("createDate", FieldType.String);
            Add("createDateTimestamp", FieldType.Int64, sort: true);
            Add("creatorName", FieldType.String);
            Add("updateDate", FieldType.String);
            Add("updateDateTimestamp", FieldType.Int64, sort: true);
            Add("writerName", FieldType.String);
            Add("templateId", FieldType.Int64, sort: true);
            Add("level", FieldType.Int64, sort: true);
            Add("path", FieldType.StringArray);
            Add("contentTypeAlias", FieldType.String, facet: true);
            Add("url", FieldType.String);

            var cultures = GetCultures();

            foreach (var culture in cultures)
            {
                Add($"name-{culture}", FieldType.String);
                Add($"url-{culture}", FieldType.String);
            }

            foreach (var contentDataItem in contentData ?? Enumerable.Empty<ContentData>())
            {
                var alias = contentDataItem?.ContentType?.Alias;
                if (string.IsNullOrWhiteSpace(alias)) continue;

                var contentType = _contentTypeService.Get(alias);
                if (contentType == null) continue;

                foreach (var selectedProperty in contentDataItem.Properties ?? Enumerable.Empty<ContentEntity>())
                {
                    var propertyType = contentType.CompositionPropertyTypes
                        .FirstOrDefault(p => p.Alias == selectedProperty?.Alias);
                    if (propertyType == null) continue;

                    var fieldType = ResolveFieldType(propertyType.PropertyEditorAlias);

                    if (propertyType.VariesByCulture())
                    {
                        foreach (var culture in cultures)
                        {
                            Add($"{propertyType.Alias}-{culture}", fieldType);
                        }
                    }
                    else
                    {
                        Add(propertyType.Alias, fieldType);
                    }
                }
            }

            // Last, so the explicit definitions above win and anything else (custom record builders,
            // the geolocation field) is still picked up.
            fields.Add(new Field(".*", FieldType.Auto));

            return fields;
        }

        /// <summary>
        /// Maps a property editor onto the Typesense type that
        /// <see cref="TypesenseSearchPropertyIndexValueFactory"/> will produce for it. Properties
        /// without a registered converter go through the default path, which always emits an array
        /// of strings. A converter that does not declare its type falls back to "auto".
        /// </summary>
        private FieldType ResolveFieldType(string propertyEditorAlias)
        {
            var converter = _converterCollection.FirstOrDefault(p => p.Name == propertyEditorAlias);
            if (converter == null) return FieldType.StringArray;

            return converter is ITypesenseFieldTypeProvider fieldTypeProvider
                ? fieldTypeProvider.GetFieldType()
                : FieldType.Auto;
        }

        private List<string> GetCultures() =>
            _languageService.GetAllAsync().GetAwaiter().GetResult()
                .Select(p => p.IsoCode)
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .ToList();
    }
}
