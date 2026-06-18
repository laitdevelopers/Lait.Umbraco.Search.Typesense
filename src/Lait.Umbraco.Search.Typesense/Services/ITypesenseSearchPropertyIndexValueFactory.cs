using Umbraco.Cms.Core.Models;

namespace Umbraco.Cms.Integrations.Search.Typesense.Services
{
    public interface ITypesenseSearchPropertyIndexValueFactory
    {
        /// <summary>
        /// Gets a property's indexed value.
        /// </summary>
        /// <returns>An [alias, value] pair.</returns>
        KeyValuePair<string, object> GetValue(IProperty property, string culture);
    }
}
