using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Integrations.Search.Typesense.Extensions;

namespace Umbraco.Cms.Integrations.Search.Typesense.Converters
{
    public class UmbracoDecimalConverter : ITypesenseIndexValueConverter
    {
        public string Name => Core.Constants.PropertyEditors.Aliases.Decimal;

        public object ParseIndexValues(IProperty property) =>
            property.TryGetPropertyIndexValue(out string value)
                ? (decimal.TryParse(value.ToString(), out var result)
                    ? result
                    : default)
                : default;
    }
}
