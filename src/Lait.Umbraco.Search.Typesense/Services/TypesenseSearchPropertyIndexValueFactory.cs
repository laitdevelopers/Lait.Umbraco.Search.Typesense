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
            var availableCultures = _languageService.GetAllAsync().GetAwaiter().GetResult()
                .Select(p => p.IsoCode);
            IDictionary<Guid, IContentType> contentTypeDictionary = _contentTypeService.GetAll().ToDictionary(x => x.Key);

            var propertyEditor = _propertyEditorsCollection
                .FirstOrDefault(p => p.Alias == property.PropertyType.PropertyEditorAlias);
            if (propertyEditor == null)
            {
                return default;
            }

            var converter = _converterCollection
                .FirstOrDefault(p => p.Name == property.PropertyType.PropertyEditorAlias);
            if (converter != null)
            {
                var result = converter.ParseIndexValues(property);
                return new KeyValuePair<string, object>(property.Alias, result);
            }

            IEnumerable<IndexValue> indexValues =
                propertyEditor.PropertyIndexValueFactory.GetIndexValues(
                    property,
                    culture,
                    null,
                    true,
                    availableCultures,
                    contentTypeDictionary);

            if (indexValues == null || !indexValues.Any())
                return new KeyValuePair<string, object>(property.Alias, string.Empty);

            var indexValue = indexValues.First();

            return new KeyValuePair<string, object>(property.Alias, indexValue.Values);
        }
    }
}
