using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Integrations.Search.Typesense.Extensions;

namespace Umbraco.Cms.Integrations.Search.Typesense.Converters
{
    public class UmbracoBooleanConverter : ITypesenseIndexValueConverter
    {
        public string Name => Core.Constants.PropertyEditors.Aliases.Boolean;

        public object ParseIndexValues(IProperty property) =>
            property.TryGetPropertyIndexValue(out string value)
                ? value.Equals("1")
                : default;
    }
}
