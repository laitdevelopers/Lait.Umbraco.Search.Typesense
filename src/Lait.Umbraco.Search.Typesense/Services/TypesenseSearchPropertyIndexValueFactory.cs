using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Integrations.Search.Typesense.Providers;

namespace Umbraco.Cms.Integrations.Search.Typesense.Services
{
    public class TypesenseSearchPropertyIndexValueFactory : ITypesenseSearchPropertyIndexValueFactory
    {
        private readonly PropertyEditorCollection _propertyEditorsCollection;

        private readonly ConverterCollection _converterCollection;

        private readonly ILanguageService _languageService;

        private readonly IContentTypeService _contentTypeService;

        public TypesenseSearchPropertyIndexValueFactory(
            PropertyEditorCollection propertyEditorCollection,
            ConverterCollection converterCollection,
            ILanguageService languageService,
            IContentTypeService contentTypeService)
        {
            _propertyEditorsCollection = propertyEditorCollection;
            _converterCollection = converterCollection;
            _languageService = languageService;
            _contentTypeService = contentTypeService;
        }

        public virtual KeyValuePair<string, object> GetValue(IProperty property, string culture)
        {
            var propertyEditor = _propertyEditorsCollection
                .FirstOrDefault(p => p.Alias == property.PropertyType.PropertyEditorAlias);
            if (propertyEditor == null)
            {
                // No editor to read the value with. Return an empty value rather than a default
                // KeyValuePair, whose null key would throw when added to the record's data.
                return new KeyValuePair<string, object>(property.Alias, new List<string>());
            }

            var converter = _converterCollection
                .FirstOrDefault(p => p.Name == property.PropertyType.PropertyEditorAlias);
            if (converter != null)
            {
                var result = converter.ParseIndexValues(property);
                return new KeyValuePair<string, object>(property.Alias, result);
            }

            var availableCultures = _languageService.GetAllAsync().GetAwaiter().GetResult()
                .Select(p => p.IsoCode);
            IDictionary<Guid, IContentType> contentTypeDictionary = _contentTypeService.GetAll().ToDictionary(x => x.Key);

            IEnumerable<IndexValue> indexValues =
                propertyEditor.PropertyIndexValueFactory.GetIndexValues(
                    property,
                    culture,
                    null,
                    true,
                    availableCultures,
                    contentTypeDictionary);

            return new KeyValuePair<string, object>(property.Alias, Flatten(indexValues));
        }

        /// <summary>
        /// Flattens an editor's index values into a list of strings.
        /// </summary>
        /// <remarks>
        /// The shape matters: collections declare these fields as "string[]", and Typesense rejects
        /// the whole document if a field arrives as anything else. The previous implementation
        /// returned the raw <see cref="IndexValue.Values"/> when the editor produced something and a
        /// bare empty string when it did not, so an empty property produced either <c>[null]</c> or
        /// <c>""</c> where the field was declared as an array of strings. Nulls and blanks are
        /// dropped, leaving an empty array, which Typesense accepts.
        /// </remarks>
        private static List<string> Flatten(IEnumerable<IndexValue> indexValues)
        {
            var values = new List<string>();

            var indexValue = indexValues?.FirstOrDefault();
            if (indexValue?.Values == null) return values;

            foreach (var value in indexValue.Values)
            {
                var text = value?.ToString();
                if (!string.IsNullOrWhiteSpace(text)) values.Add(text);
            }

            return values;
        }
    }
}
