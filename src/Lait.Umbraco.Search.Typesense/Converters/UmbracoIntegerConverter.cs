using global::Typesense;

using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Integrations.Search.Typesense.Extensions;

namespace Umbraco.Cms.Integrations.Search.Typesense.Converters
{
    public class UmbracoIntegerConverter : ITypesenseIndexValueConverter, ITypesenseFieldTypeProvider
    {
        public string Name => Core.Constants.PropertyEditors.Aliases.Integer;

        public object ParseIndexValues(IProperty property) =>
            property.TryGetPropertyIndexValue(out string value)
                ? (int.TryParse(value.ToString(), out var result)
                    ? result
                    : default)
                : default;

        public FieldType GetFieldType() => FieldType.Int64;
    }
}
