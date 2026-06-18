using System.Text.Json;

using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Integrations.Search.Typesense.Extensions;

namespace Umbraco.Cms.Integrations.Search.Typesense.Converters
{
    public class UmbracoTagsConverter : ITypesenseIndexValueConverter
    {
        public string Name => Core.Constants.PropertyEditors.Aliases.Tags;

        public object ParseIndexValues(IProperty property)
        {
            if (!property.TryGetPropertyIndexValue(out string value))
            {
                return Enumerable.Empty<string>();
            }

            List<string> valuesArr;
            try
            {
                valuesArr = JsonSerializer.Deserialize<List<string>>(value);
            }
            catch (JsonException)
            {
                // Fallback: split the comma-separated string manually
                valuesArr = value?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList()
                    ?? new List<string>();
            }

            if (valuesArr != null && valuesArr.Any())
            {
                return valuesArr.Select(p => p);
            }

            return Enumerable.Empty<string>();
        }
    }
}
